using KnapsackProblem;

var items = new MyItem[3];
items[0] = new MyItem(1);
items[1] = new MyItem(2);
items[2] = new MyItem(3);
var res = UnboundedKnapsack.SolveFloatApproximateAsDP(items, 10, (a) => a.Weight(), 1);
foreach (var item in res)
{
    Console.WriteLine(item.weight);
}

class MyItem : IKnapsackItem
{
    public float weight;

    public MyItem(float weight)
    {
        this.weight = weight;
    }

    public float Weight()
    {
        return weight;
    }
}