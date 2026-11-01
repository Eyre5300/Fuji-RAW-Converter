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
 // A batch has a stable target task count, not an on/off memory threshold.
 // One successful task calibrates the cold estimate; subsequent changes only
 // reduce concurrency after sustained pressure. Task completion never ramps it up.
 internal sealed class ParallelPolicy {
  public readonly int Requested,Maximum;
  public int Target;
  public string Reason="configured";
  readonly double initialBytes;
  double externalPhysical,externalCommit,peakPhysical,peakCommit;
  bool initialized,calibrated,completedSample;
  DateTime lastSample,lowerSince;
  public ParallelPolicy(int requested,int total,long initialTaskBytes){Requested=requested;Maximum=Math.Min(requested,total);Target=Maximum;initialBytes=initialTaskBytes;}
  public void Observe(double physicalPeak,double commitPeak){peakPhysical=Math.Max(peakPhysical,physicalPeak);peakCommit=Math.Max(peakCommit,commitPeak);}
  public void Completed(){if(peakPhysical>0&&peakCommit>0)completedSample=true;}
  public void Update(MemoryPressure memory,double workerPhysical,double workerCommit,DateTime now){
   if(!memory.Valid){Reason=Target<Maximum?"memory_budget":"memory_unavailable";return;}
   double outsidePhysical=Math.Max(0,(double)memory.TotalPhysical-memory.AvailablePhysical-workerPhysical);
   double outsideCommit=Math.Max(0,(double)memory.TotalCommit-memory.AvailableCommit-workerCommit);
   bool first=!initialized;
   if(first){externalPhysical=outsidePhysical;externalCommit=outsideCommit;initialized=true;}
   else {
    // Filter accounting/cache fluctuations across task boundaries (8-second time constant).
    double alpha=1-Math.Exp(-Math.Max(0,(now-lastSample).TotalSeconds)/8);
    externalPhysical+=alpha*(outsidePhysical-externalPhysical);externalCommit+=alpha*(outsideCommit-externalCommit);
   }
   lastSample=now;
   bool calibrate=completedSample&&!calibrated;
   if(calibrate)calibrated=true;
   double physicalTask=Math.Max(calibrated?128L*1024*1024:initialBytes,peakPhysical*1.10);
   double commitTask=Math.Max(calibrated?128L*1024*1024:initialBytes,peakCommit*1.10);
   double physicalSlots=(memory.TotalPhysical*0.90-externalPhysical)/physicalTask;
   double commitSlots=(memory.TotalCommit*0.90-externalCommit)/commitTask;
   int candidate=(int)Math.Max(1,Math.Min(Maximum,Math.Floor(Math.Min(physicalSlots,commitSlots))));
   if(first||calibrate){Target=candidate;lowerSince=DateTime.MinValue;}
   else if(candidate<Target){
    if(lowerSince==DateTime.MinValue)lowerSince=now;
    // Do not chase short-lived peaks; rapidly growing severe pressure gets a shorter window.
    double hold=memory.Physical>=95||memory.Commit>=95?1:3;
    if((now-lowerSince).TotalSeconds>=hold){Target=candidate;lowerSince=DateTime.MinValue;}
   }else lowerSince=DateTime.MinValue;
   Reason=Target<Maximum?"memory_budget":"configured";
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
  public AdaptiveParallelism(int requested,int count,long initialTaskBytes,Func<MemoryPressure> read,Action<ParallelState> notify){policy=new ParallelPolicy(requested,count,initialTaskBytes);total=count;readMemory=read;report=notify;}
  void Refresh(){
   DateTime now=DateTime.UtcNow;if((now-lastRefresh).TotalMilliseconds<250)return;lastRefresh=now;
   double physical=0,commit=0;
   foreach(var worker in workers.Values){worker.Sample();physical+=worker.Physical;commit+=worker.Commit;policy.Observe(worker.PeakPhysical,worker.PeakCommit);}
   memory=readMemory();policy.Update(memory,physical,commit,now);
  }
  void Report(){
   string key=policy.Target+"/"+active+"/"+next+"/"+policy.Reason+"/"+memory.Physical+"/"+memory.Commit;
   if(key==lastReport)return;lastReport=key;
   report(new ParallelState{requested=policy.Requested,target=policy.Target,active=active,remaining=total-next,reason=policy.Reason,memoryAvailable=memory.Valid,physicalPercent=memory.Physical,commitPercent=memory.Commit});
  }
  public void Track(Process process){lock(gate){workers.Add(process,new Worker{Process=process});}}
  public void Untrack(Process process,bool succeeded){lock(gate){Worker worker;if(workers.TryGetValue(process,out worker)){worker.Sample();policy.Observe(worker.PeakPhysical,worker.PeakCommit);workers.Remove(process);if(succeeded)policy.Completed();}}}
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
