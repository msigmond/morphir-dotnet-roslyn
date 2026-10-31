using System;using System.Numerics;
namespace Example.Parity;
public readonly record struct Loan(string SecurityId,int Quantity,int Price);
public readonly record struct LoanFee(string SecurityId,int MarketValue);
public static class Rules
{
public static LoanFee Create(Loan loan)=>new(loan.SecurityId,loan.Quantity*loan.Price);
public static int Read(Loan loan)=>loan.Quantity;
}
