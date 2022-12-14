namespace ExampleProject;

public class ExampleClass2
{
    private IKitten _num;
    private IPuppy _bebra;
    private int _chislo;

    public ExampleClass2(IKitten num, IPuppy bebra, int chislo)
    {
        _num = num;
        _bebra = bebra;
        _chislo = chislo;
    }
    
    public int Method1(int num1, int num2)
    {
        return num1 + num2;
    }

    public void Method2(int n)
    {
        return;
    }

    private void HiddenMethod()
    {
        return;
    }
}