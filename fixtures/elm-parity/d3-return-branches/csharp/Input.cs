namespace Example.Completion;public static class Rules{
 public static int Explicit(bool b,int a,int c){if(b){return a;}else{return c;}}
 public static int Early(bool b,int a,int c){if(b)return a;return c;}
 public static int Nested(bool a,bool b){if(a){if(b)return 1;return 2;}else{return 3;}}
 public static int Locals(bool b,int a){if(b){int x=a+1;return x;}else{int x=a*2;return x;}}
 public static int Constant(){if(false)return 2;return 1+3;}
 public static int Checked(int a){checked{return a+1;}}
 }
