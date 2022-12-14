using System.Collections;

namespace ExampleProject
{
    namespace FirstExample
    {

        public class ExampleClass
        {
            private int _param1;
            private string[] _param2;

            public int Param1
            {
                get => _param1;
                set => _param1 = value;
            }

            public string[] Param2
            {
                get => _param2;
                set => _param2 = value;
            }

            public int Sum(int num1, int num2)
            {
                return num1 + num2;
            }

            public int Des(int num1, int num2)
            {
                return num1 - num2;
            }

            public int Mul(int num1, int num2)
            {
                return num1 * num2;
            }

            private void HiddenMethod()
            {
                return;
            }
        }
    }
}
