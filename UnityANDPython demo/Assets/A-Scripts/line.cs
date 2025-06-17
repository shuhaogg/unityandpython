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
        //1.�����״ͼ
        var chart = gameObject.GetComponent<BarChart>();
        if (chart == null)
        {
            chart = gameObject.AddComponent<BarChart>();
            chart.Init();
        }

        //2.������С�����붯̬���óߴ磬��ֱ�Ӳ���chart.rectTransform����ֱ����Inspector�ϸ�
        chart.SetSize(580, 300);

        //3.���ñ���
        var title = chart.EnsureChartComponent<Title>();
        title.text = "����״ͼ";

        //4.������ʾ���ͼ���Ƿ���ʾ
        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show = false;

        var legend = chart.EnsureChartComponent<Legend>();
        legend.show = false;


        //5.����������
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.splitNumber = 10;
        xAxis.boundaryGap = true;
        xAxis.type = Axis.AxisType.Category;

        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;


        //6.���Ĭ�����ݣ����Bar���͵�Serie���ڽ�������
        chart.RemoveData();
        chart.AddSerie<Bar>("bar");


        //7.���10������
        for (int i = 0; i < 10; i++)
        {
            chart.AddXAxisData("x" + i);
            chart.AddData(0, Random.Range(10, 200));
        }
    }
}