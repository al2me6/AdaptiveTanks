using KnapsackProblem;

var items = new MyItem[3];
items[0] = new MyItem(1.9f, 1);
items[1] = new MyItem(2.9f, 2);
items[2] = new MyItem(3.9f, 3);
var res = UnboundedKnapsack.SolveFloatApproximateAsDP(items, 11, (a) => a.value, 1);
foreach (var item in res)
{
    Console.WriteLine(item.weight);
}

class MyItem : IKnapsackItem
{
    public float weight;
    public float value;

    public MyItem(float weight, float value)
    {
        this.weight = weight;
        this.value = value;
    }

    public float Weight()
    {
        return weight;
    }
}