namespace Example.Completion {
 public static class Rules {
  private record Secret(int Code);
  private static int Read(Secret data)=>data.Code;
  public static int Compute(int @event)=>Read(new Secret(@event));
 }
}
namespace Example.CompletionSuffix {public static class Outside {public static object Unmapped()=>new object();}}
