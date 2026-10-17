using System.Numerics;
using System.Text.Json;
using System.Globalization;
if(Environment.Version!=new Version(10,0,0) || IntPtr.Size!=8)throw new InvalidOperationException("Pinned .NET10.0.0 x64 runtime required");
object Bits(decimal value){var bits=decimal.GetBits(value);var coefficient=new BigInteger((uint)bits[0])+((BigInteger)(uint)bits[1]<<32)+((BigInteger)(uint)bits[2]<<64);var digits=new List<int>();while(coefficient!=0){digits.Add((int)(coefficient&65535));coefficient>>=16;}return new{coefficient=new{digits,negative=false},scale=(bits[3]>>16)&255,negative=bits[3]<0};}
var values=new List<decimal>{0m,-0.00m,0.00m,1m,1.00m,-1m,2m,2.0m,3m,0.0000000000000000000000000000m,0.0000000000000000000000000001m,0.0000000000000000000000000002m,-0.0000000000000000000000000001m,decimal.MaxValue,decimal.MinValue,7922816251426433759354395033.5m,1.2345678901234567890123456789m,2.5m,-2.5m,3.5m,-3.5m};
var random=new Random(0xD6);for(int i=0;i<24;i++)values.Add(new decimal(random.Next(int.MinValue,int.MaxValue),random.Next(int.MinValue,int.MaxValue),random.Next(int.MinValue,int.MaxValue),i%2==0,(byte)(i%29)));
var cases=new List<object>();var expected=new List<object>();
void Add(string operation,decimal a,decimal b,int digits=0,int mode=0){try{var result=operation switch{"add"=>a+b,"subtract"=>a-b,"multiply"=>a*b,"divide"=>a/b,"remainder"=>a%b,_=>decimal.Round(a,digits,(MidpointRounding)mode)};cases.Add(new{operation,left=Bits(a),right=Bits(b),digits,mode});expected.Add(Bits(result));}catch(ArithmeticException){}}
foreach(var a in values){foreach(var b in values.Take(15).Concat(values.Skip(13).Where((_,index)=>index%3==0))){foreach(var op in new[]{"add","subtract","multiply","divide","remainder"})Add(op,a,b);}foreach(var digits in new[]{0,2,28})for(int mode=0;mode<=4;mode++)Add("round",a,0,digits,mode);}
File.WriteAllText("artifacts/decimal-clr-cases.json",JsonSerializer.Serialize(new{cases,expected}));Console.WriteLine($"Generated {cases.Count} native CLR Decimal coefficient/scale/sign cases.");
