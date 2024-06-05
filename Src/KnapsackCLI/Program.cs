using KnapsackProblem;

var items = new MyItem[3];
items[0] = new MyItem(1.9f, 1);
items[1] = new MyItem(2.9f, 2);
items[2] = new MyItem(3.9f, 3);
var maxWeight = 11.6f;
var res = UnboundedKnapsack.SolveFloatApproximateAsDP(items, maxWeight, (a) => a.value, 1);
foreach (var item in res)
{
    Console.WriteLine(item.weight);
}
var total = res.Select(x => x.weight).Sum();
Console.WriteLine($"Max weight: {maxWeight}");
Console.WriteLine($"Weight of solution: {total}");

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