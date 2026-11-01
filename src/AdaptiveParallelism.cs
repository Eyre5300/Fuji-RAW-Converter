using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace S5Raw {
 internal struct MemoryPressure {
  public bool Valid;
  public int Physical,Commit;
  public ulong TotalPhysical,AvailablePhysical,TotalCommit,AvailableCommit;
  [StructLayout(LayoutKind.Sequential)] struct Status {
   public uint Length,Load;
   public ulong TotalPhysical,AvailablePhysical,TotalCommit,AvailableCommit,TotalVirtual,AvailableVirtual,Extended;
  }
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx(ref Status status);
  public static MemoryPressure Read(){
   var s=new Status();s.Length=(uint)Marshal.SizeOf(typeof(Status));
   if(!GlobalMemoryStatusEx(ref s)||s.TotalPhysical==0||s.TotalCommit==0)return new MemoryPressure();
   return new MemoryPressure{Valid=true,TotalPhysical=s.TotalPhysical,AvailablePhysical=s.AvailablePhysical,TotalCommit=s.TotalCommit,AvailableCommit=s.AvailableCommit,Physical=(int)(100*(1-(double)s.AvailablePhysical/s.TotalPhysical)),Commit=(int)(100*(1-(double)s.AvailableCommit/s.TotalCommit))};
  }
 }
 // Measure one complete conversion before admitting parallel work. There are no
 // fixed per-camera memory estimates. After calibration, actual memory provides
 // bidirectional feedback with a dead band and settling time.
 internal sealed class ParallelPolicy {
  public readonly int Requested,Maximum;
  public int Target;
  public string Reason="profiling";
  double profilePhysical,profileCommit,taskPhysical,taskCommit;
  bool profileReady,calibrated;
  DateTime lastSample,lastAdjustment,lowSince,highSince;
  public ParallelPolicy(int requested,int total){Requested=requested;Maximum=Math.Min(requested,total);Target=Math.Min(1,Maximum);}
  public void Completed(double physicalPeak,double commitPeak){
   if(!profileReady&&physicalPeak>0&&commitPeak>0){profilePhysical=physicalPeak;profileCommit=commitPeak;profileReady=true;}
  }
  int Clamp(double value){return (int)Math.Max(1,Math.Min(Maximum,Math.Floor(value)));}
  void SetReason(){Reason=!calibrated?"profiling":Target<Maximum?"memory_budget":"configured";}
  public void Update(MemoryPressure memory,double workerPhysical,double workerCommit,int workers,int active,int pending,DateTime now){
   double elapsed=lastSample==DateTime.MinValue?0:Math.Max(0,(now-lastSample).TotalSeconds);lastSample=now;
   if(!memory.Valid){lowSince=highSince=DateTime.MinValue;SetReason();return;}
   double physicalUsed=(double)memory.TotalPhysical-memory.AvailablePhysical;
   double commitUsed=(double)memory.TotalCommit-memory.AvailableCommit;
   if(!calibrated){
    if(!profileReady)return;
    taskPhysical=profilePhysical;taskCommit=profileCommit;
    double externalPhysical=Math.Max(0,physicalUsed-workerPhysical);
    double externalCommit=Math.Max(0,commitUsed-workerCommit);
    Target=Clamp(Math.Min((memory.TotalPhysical*0.90-externalPhysical)/taskPhysical,(memory.TotalCommit*0.90-externalCommit)/taskCommit));
    calibrated=true;lastAdjustment=now;SetReason();return;
   }
   if(workers>0&&workerPhysical>0&&workerCommit>0){
    // Rolling observations replace the initial peak; an old spike cannot lock
    // concurrency low for the remainder of the batch.
    double alpha=1-Math.Exp(-elapsed/5);
    taskPhysical+=alpha*(workerPhysical/workers-taskPhysical);
    taskCommit+=alpha*(workerCommit/workers-taskCommit);
   }
   // Reserve measured memory for tasks still copying input / starting a process.
   double projectedPhysical=physicalUsed+Math.Max(0,active-workers)*taskPhysical;
   double projectedCommit=commitUsed+Math.Max(0,active-workers)*taskCommit;
   double physicalRatio=projectedPhysical/memory.TotalPhysical;
   double commitRatio=projectedCommit/memory.TotalCommit;
   double spareTasks=Math.Min((memory.TotalPhysical*0.90-projectedPhysical)/taskPhysical,(memory.TotalCommit*0.90-projectedCommit)/taskCommit);
   bool high=physicalRatio>0.92||commitRatio>0.92;
   bool low=physicalRatio<0.88&&commitRatio<0.88;
   // Let a previous reduction drain before making another adjustment. A short
   // queue at the end of the batch is not evidence that more workers are needed.
   if(active>Target){lowSince=highSince=DateTime.MinValue;SetReason();return;}
   if(high&&active==Target){
    lowSince=DateTime.MinValue;if(highSince==DateTime.MinValue)highSince=now;
    bool severe=physicalRatio>=0.95||commitRatio>=0.95;
    if((now-highSince).TotalSeconds>=(severe?1:3)&&(severe||(now-lastAdjustment).TotalSeconds>=5)){
     int candidate=Clamp(active+Math.Floor(spareTasks));
     if(candidate<Target){Target=candidate;lastAdjustment=now;}highSince=DateTime.MinValue;
    }
   }else if(low&&pending>0&&active==Target&&workers>0&&Target<Maximum){
    highSince=DateTime.MinValue;if(lowSince==DateTime.MinValue)lowSince=now;
    if((now-lowSince).TotalSeconds>=5&&(now-lastAdjustment).TotalSeconds>=5){
     int candidate=Clamp(active+Math.Floor(spareTasks));
     // Increase towards the measured estimate in bounded steps, then observe
     // the new workers for five seconds instead of instantly filling all slots.
     int step=Math.Max(1,(int)Math.Ceiling(Target*0.25));
     if(candidate>Target){Target=Math.Min(candidate,Target+step);lastAdjustment=now;}lowSince=DateTime.MinValue;
    }
   }else lowSince=highSince=DateTime.MinValue;
   SetReason();
  }
 }
 internal sealed class ParallelState {
  public string type="parallel_status",reason;
  public int requested,target,active,remaining,physicalPercent,commitPercent;
  public bool memoryAvailable;
 }
 internal sealed class AdaptiveParallelism {
  sealed class Worker {
   public Process Process;
   public long Physical,Commit,PeakPhysical,PeakCommit;
   public void Sample(){try{Process.Refresh();Physical=Process.WorkingSet64;Commit=Process.PrivateMemorySize64;PeakPhysical=Math.Max(PeakPhysical,Process.PeakWorkingSet64);PeakCommit=Math.Max(PeakCommit,Commit);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){} }
  }
  readonly object gate=new object();
  readonly ParallelPolicy policy;
  readonly int total;
  readonly Func<MemoryPressure> readMemory;
  readonly Action<ParallelState> report;
  readonly Dictionary<Process,Worker> workers=new Dictionary<Process,Worker>();
  int next,active;
  MemoryPressure memory;
  DateTime lastRefresh;
  string lastReport;
  public AdaptiveParallelism(int requested,int count,Func<MemoryPressure> read,Action<ParallelState> notify){policy=new ParallelPolicy(requested,count);total=count;readMemory=read;report=notify;}
  void Refresh(){
   DateTime now=DateTime.UtcNow;if((now-lastRefresh).TotalMilliseconds<250)return;lastRefresh=now;
   double physical=0,commit=0;
   foreach(var worker in workers.Values){worker.Sample();physical+=worker.Physical;commit+=worker.Commit;}
   memory=readMemory();policy.Update(memory,physical,commit,workers.Count,active,total-next,now);
  }
  void Report(){
   string key=policy.Target+"/"+active+"/"+next+"/"+policy.Reason+"/"+memory.Physical+"/"+memory.Commit;
   if(key==lastReport)return;lastReport=key;
   report(new ParallelState{requested=policy.Requested,target=policy.Target,active=active,remaining=total-next,reason=policy.Reason,memoryAvailable=memory.Valid,physicalPercent=memory.Physical,commitPercent=memory.Commit});
  }
  public void Track(Process process){lock(gate){workers.Add(process,new Worker{Process=process});}}
  public void Untrack(Process process,bool succeeded){lock(gate){Worker worker;if(workers.TryGetValue(process,out worker)){worker.Sample();workers.Remove(process);if(succeeded)policy.Completed(worker.PeakPhysical,worker.PeakCommit);}}}
  public void Tick(){lock(gate){Refresh();Report();Monitor.PulseAll(gate);}}
  public int Take(Func<bool> cancelled){lock(gate){
   while(!cancelled()&&next<total){
    Refresh();
    if(active<policy.Target){int index=next++;active++;Report();return index;}
    Report();Monitor.Wait(gate,250);
   }
   return -1;
  }}
  public void Complete(){lock(gate){active--;Refresh();Report();Monitor.PulseAll(gate);}}
 }
}
