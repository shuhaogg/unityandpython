using UnityEngine;
using XCharts.Runtime;

public class XChartsTest : MonoBehaviour
{
    void Start()
    {
        CreateCharts();
    }

    void CreateCharts()
    {
        //1.添加柱状图
        var chart = gameObject.GetComponent<BarChart>();
        if (chart == null)
        {
            chart = gameObject.AddComponent<BarChart>();
            chart.Init();
        }

        //2.调整大小。代码动态设置尺寸，或直接操作chart.rectTransform，或直接在Inspector上改
        chart.SetSize(580, 300);

        //3.设置标题
        var title = chart.EnsureChartComponent<Title>();
        title.text = "简单柱状图";

        //4.设置提示框和图例是否显示
        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show = false;

        var legend = chart.EnsureChartComponent<Legend>();
        legend.show = false;


        //5.设置坐标轴
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.splitNumber = 10;
        xAxis.boundaryGap = true;
        xAxis.type = Axis.AxisType.Category;

        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;


        //6.清空默认数据，添加Bar类型的Serie用于接收数据
        chart.RemoveData();
        chart.AddSerie<Bar>("bar");


        //7.添加10个数据
        for (int i = 0; i < 10; i++)
        {
            chart.AddXAxisData("x" + i);
            chart.AddData(0, Random.Range(10, 200));
        }
    }
}