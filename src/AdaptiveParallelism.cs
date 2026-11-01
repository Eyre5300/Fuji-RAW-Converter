using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace S5Raw {
 internal struct MemoryPressure {
  public bool Valid;
  public int Physical,Commit;
  [StructLayout(LayoutKind.Sequential)] struct Status {
   public uint Length,Load;
   public ulong TotalPhysical,AvailablePhysical,TotalCommit,AvailableCommit,TotalVirtual,AvailableVirtual,Extended;
  }
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx(ref Status status);
  public static MemoryPressure Read(){
   var s=new Status();s.Length=(uint)Marshal.SizeOf(typeof(Status));
   if(!GlobalMemoryStatusEx(ref s)||s.TotalPhysical==0||s.TotalCommit==0)return new MemoryPressure();
   return new MemoryPressure{Valid=true,Physical=(int)(100*(1-(double)s.AvailablePhysical/s.TotalPhysical)),Commit=(int)(100*(1-(double)s.AvailableCommit/s.TotalCommit))};
  }
 }
 // The configured value never changes. Memory protection is reported separately.
 internal sealed class ParallelPolicy {
  public readonly int Requested,Maximum;
  public int Limit;
  public string Reason="configured";
  bool pressure;
  DateTime lastIncrease;
  public ParallelPolicy(int requested,int total){Requested=requested;Maximum=Math.Min(requested,total);Limit=Maximum;}
  public void Update(MemoryPressure memory,DateTime now){
   if(!memory.Valid){Limit=Maximum;pressure=false;Reason="memory_unavailable";return;}
   if(memory.Physical>=90||memory.Commit>=90){pressure=true;Limit=0;Reason="memory_pressure";lastIncrease=now;return;}
   if(pressure){
    if(memory.Physical>80||memory.Commit>80){Reason="memory_pressure";return;}
    pressure=false;Limit=Math.Min(1,Maximum);lastIncrease=now;
   }else if(Limit<Maximum&&(now-lastIncrease).TotalSeconds>=2){Limit++;lastIncrease=now;}
   Reason=Limit<Maximum?"recovering":"configured";
  }
 }
 internal sealed class ParallelState {
  public string type="parallel_status",reason;
  public int requested,limit,active,remaining,physicalPercent,commitPercent;
  public bool memoryAvailable;
 }
 internal sealed class AdaptiveParallelism {
  readonly object gate=new object();
  readonly ParallelPolicy policy;
  readonly int total;
  readonly Func<MemoryPressure> readMemory;
  readonly Action<ParallelState> report;
  int next,active;
  MemoryPressure memory;
  string lastReport;
  public AdaptiveParallelism(int requested,int count,Func<MemoryPressure> read,Action<ParallelState> notify){policy=new ParallelPolicy(requested,count);total=count;readMemory=read;report=notify;}
  void Refresh(){memory=readMemory();policy.Update(memory,DateTime.UtcNow);}
  void Report(){
   string key=policy.Limit+"/"+active+"/"+next+"/"+policy.Reason+"/"+memory.Physical+"/"+memory.Commit;
   if(key==lastReport)return;lastReport=key;
   report(new ParallelState{requested=policy.Requested,limit=policy.Limit,active=active,remaining=total-next,reason=policy.Reason,memoryAvailable=memory.Valid,physicalPercent=memory.Physical,commitPercent=memory.Commit});
  }
  public void Tick(){lock(gate){Refresh();Report();Monitor.PulseAll(gate);}}
  public int Take(Func<bool> cancelled){lock(gate){
   while(!cancelled()&&next<total){
    Refresh();
    if(active<policy.Limit){int index=next++;active++;Report();return index;}
    Report();Monitor.Wait(gate,250);
   }
   return -1;
  }}
  public void Complete(){lock(gate){active--;Refresh();Report();Monitor.PulseAll(gate);}}
 }
}
