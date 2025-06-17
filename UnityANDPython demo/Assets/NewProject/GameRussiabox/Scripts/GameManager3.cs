/// 1.俄罗斯方块
using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public class GameManager3 : MonoSingleton<GameManager3>
{
    //一个计时工具  需要将 PETimer这个dll放入项目里   
    public PETimer timer;
    //当前正在下落的那个物体的引用
    public Shape current;
    //存放各种形状的预制体，长条 田字型 山型... 用于随机生成物体用
    public GameObject[] shapePrefabs;
    //预设的一组颜色 同用于随机
    public Color[] colors;
    //存放场景里的小格子 注意这里存放的是 一个物体里面的单个的小格子， 不是一整个shape
    public GameObject[,] blocks;
    //自动下落的速度  可用于作为难度的参数
    public float speed;
    float time;

    //背景格子的父物体  后续有个dotween动画好像可能要用到他
    public GameObject background;
    //游戏是否开始  同时也用于判断是否还在游戏中 游戏是否结束
    public bool isGameStart;
    //记录当前分数的变量
    public int score;
    //存放下落的物体的父物体  让Hierarchy界面整洁一些
    public Transform shapeParent;
    private void Awake()
    {
        //为了测试方便 直接在代码里给了个速度    
        speed = 0.45f;
        //数组Y轴稍微给大了一点  感觉用20 需要对一些情况做些判断 但是多给几行的话 可以省去这些功夫 如果不理解也无所谓
        blocks = new GameObject[10, 24];
        //PETimer如何使用 可以看siki学院上的免费课程  定时回调技术专题
        //我来简单将一下如何使用 其实我也只会用一个API 因为没看课程
        //第一步 将PETimer这个dll放入项目里   
        //第二步 定义一个 PEtimer字段  然后随便找一个update 调用timer.update  我习惯将PETimer的实例 命名为timer
        //第三步 如果想延时执行某个函数 就可以使用timer里的方法  具体怎么用可以看UI管理器里的 StartGameBtn()函数 
        timer = new PETimer();
    }
    private void Start()
    {   
        //开始游戏时的动画  通过修改摄像机的size属性来实现
        Camera.main.DOOrthoSize(14.1f, 0.8f);
        Invoke("Delay", 0.2f);
    }
    public void Delay()
    {
        background.SetActive(true);
    }


    private void Update()
    {
        timer.Update();
        
        if (isGameStart)
        {        
            //当时间累积到 speed 也就是刚刚设定的0.45时  就下落一个单位  然后将时间归零 重新累积
            time += Time.deltaTime;
            if (time > speed)
            {
                time = 0;
                Down();
            }
            //按左键左移一格  右键右移一格 下键下移一格  按住下键就快速下移    上键是旋转物体
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ToLeft();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ToRight();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Down();
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                speed = 0.05f;
            }
            else
            {
                speed = 0.45f;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Turn();
            }
        }
      
    }   
    /// <summary>
    /// 随机生成一个物体
    /// </summary>
    /// <returns></returns>
    public Shape CreateOneShape()
    {    
        //随机形状  颜色       
        GameObject shape = Instantiate(shapePrefabs[UnityEngine.Random.Range(0, shapePrefabs.Length)]);
        shape.transform.SetParent(shapeParent);    
        Color color = colors[UnityEngine.Random.Range(0, colors.Length)];
        //找到这个物体全部子物体里的 SpriteRenderer组件  给他赋上我们随机的颜色
        SpriteRenderer[] renderers= shape.GetComponentsInChildren<SpriteRenderer>();
        foreach (var item in renderers)
        {
            item.color = color;
        }
        return shape.GetComponent<Shape>();
    }
    public bool IsGameover()
    {
        //判断一下是否已经触顶了
        for (int i = 19; i < 24; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (blocks[j, i] != null )
                {
                    isGameStart = false;
                    AudioManager.Instance.PlayLongSound("Gameover");
                    //这一块是存储一下最高的三次成绩   先将之前存的三次都取出来 在加上这次的 然后用数组的sort方法自动排序一下
                    //然后再将最高的三个分数存回去
                    List<int> scores = new List<int>();
                    scores.Add(PlayerPrefs.GetInt("highest1"));
                    scores.Add(PlayerPrefs.GetInt("highest2"));
                    scores.Add(PlayerPrefs.GetInt("highest3"));
                    scores.Add(score);
                    scores.Sort();
                    PlayerPrefs.SetInt("highest1", scores[3]);
                    PlayerPrefs.SetInt("highest2", scores[2]);
                    PlayerPrefs.SetInt("highest3", scores[1]);
                    UIManager.Instance.UpdateHighest(scores[3]);
                  
                    UIManager.Instance.gameoverScore.text = score.ToString();
                    UIManager.Instance.gameoverPanle.SetActive(true);
                    UIManager.Instance.pauseBtn.SetActive(false);
                    UIManager.Instance.exitBtn.SetActive(false);
                    return true;
                }
            }
        }
        return false;
    }
    /// <summary>
    /// 检查是否有满了的行
    /// </summary>
    bool Check()
    {
        if (IsGameover())
            return true;
        //记录满了的行的 行号 
        List<int> fullRows = new List<int>();     
        for (int y = 0; y < 20; y++)
        {
            bool isFull = true;
            for (int x = 0; x < 10; x++)
            {
                //如果这一行有一个为空 那么就设置为false  以及break   break可以跳出当前循环，节省一些性能  可以点击break 看看当前循环是哪个
                if (blocks[x, y] == null)
                {
                    isFull = false;
                    break;
                }                              
            }
            if (isFull)          
                fullRows.Add(y);                                           
        }   
        //如果存在满了的行  我们就执行 清除以及 填充的函数   
        if (fullRows.Count>0)
        {
            DeleteAndFill(fullRows);
            return true;
        }
        return false;
    }
    public void DeleteAndFill(List<int> fullRows)
    {
        //先将满了的行 销毁  并且将其在二维数组里移除掉   这里后续应该还会加上一些销毁的动画
        foreach (var item in fullRows)
        {
            for (int i = 0; i < 10; i++)
            {
                Destroy(blocks[i, item].gameObject);
               
                blocks[i, item] = null;
            }
        }
        // 1 行1000  2行3000 3行6000 4行10000 五行15000
        switch (fullRows.Count)
        {
            case 1:UIManager.Instance.AddScore(1000);
                AudioManager.Instance.PlayLongSound("Clear1");
                break;
            case 2: UIManager.Instance.AddScore(3000);
                AudioManager.Instance.PlayLongSound("Clear1");
                break;
            case 3:
                UIManager.Instance.AddScore(6000);
                AudioManager.Instance.PlayLongSound("Clear1");
                break;
            case 4:
                UIManager.Instance.AddScore(10000);
                AudioManager.Instance.PlayLongSound("Clear2");
                break;
            case 5:
                UIManager.Instance.AddScore(15000);
                AudioManager.Instance.PlayLongSound("Clear3");
                break;
            default:
                UIManager.Instance.AddScore(1000);
                AudioManager.Instance.PlayLongSound("Clear1");
                break;
        }
       
        //将上面的行下移  y随便给的 感觉最好比20大  因为或许会出现一些极端情况，已经部分超出顶部但又还未彻底游戏结束的情况  当然也得看你游戏规则是如何设定的
        for (int y = fullRows[0] + 1; y < 24; y++)
        {
            // 假设1 2 4 行满了   我们判断一下当前行的行号  假设是3  他比满了的行124 里的12大 所以就下移两行 如果是5 6 7 自然都是下移3行
            //需要注意的是 不要想当然以为有几行满 就全部下移几行就行
            for (int x = 0; x < 10; x++)
            {
                if (blocks[x, y] != null)//如果这个位置没物体 也就不必移动了 省点性能
                {
                    int count = 0;
                    foreach (var item in fullRows)
                    {
                        if (y>item)
                        {
                            count++;
                        }
                    }
                    //将position的 y-1，这里修改的是显示上的位置   然后将自己在 二维数组里的位置修改一下  注意要将自己原先在的位置置空，这里修改的是在数组里的位置
                    Vector3 pos = blocks[x, y].transform.position;
                    pos.y -= count;
                    blocks[x, y].transform.position = pos;
                    blocks[x, y -count] = blocks[x, y];                  
                    blocks[x, y] = null;
                }
            }
        }
    }
   
    //下面分别是判断能否左移 右移 下移 以及旋转  比如一根长条 他贴着左边墙了，肯定就无法旋转了 要不就超出范围了
    //再下面四个函数是 左移 右移 下移 旋转  代码很相似 但不太容易重构成一两个函数，也为了便于大家理解，就写成了8个函数
    bool CanToLeft()
    {
        //找出子物体里的 block
        foreach (Transform trans in current.transform)
        {     
            if (trans.tag=="block")
            {
                //如果已经有子物体在最左边了，那么就不能继续向左移动了
                if (trans.position.x <= 0)
                    return false;   
                //如果数组里，当前位置的左边一个单位里 没有物体 并且这个物体不是跟自己在一个父物体下的 那么就可以左移
                //如果不把跟自己一个父物体的这种情况排除的话 那么就永远不能移动了  大家可以仔细理解下
                if (blocks[Mathf.RoundToInt(trans.position.x - 1), Mathf.RoundToInt(trans.position.y)] != null && blocks[Mathf.RoundToInt(trans.position.x - 1), Mathf.RoundToInt(trans.position.y)].transform.parent!=trans.parent)
                    return false;                         
            }
        }
        return true;
    }
    bool CanToRight()
    {
        foreach (Transform trans in current.transform)
        {
            if (trans.tag == "block")
            {
                if (trans.position.x >=9)
                    return false;
                if (blocks[Mathf.RoundToInt(trans.position.x + 1), Mathf.RoundToInt(trans.position.y)] != null && blocks[Mathf.RoundToInt(trans.position.x + 1), Mathf.RoundToInt(trans.position.y)].transform.parent != trans.parent)
                    return false;
            }
        }
        return true;
    }
    bool CanDown()
    {
        foreach (Transform trans in current.transform)
        {
            if (trans.tag == "block")
            {
                if (trans.position.y <=0)
                    return false;
                if (blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y-1)] != null && blocks[Mathf.RoundToInt(trans.position.x ), Mathf.RoundToInt(trans.position.y-1)].transform.parent != trans.parent)
                    return false;
            }
        }
        return true;
    }
    bool CanTurn()
    {
        //先将物体旋转一下    这个旋转的API我之前印象也不深，临时百度的，大家不理解的话 知道还有这么一个API就行了
        // 第一个参数是绕哪个点旋转  第二个参数是绕哪个轴旋转  第三个参数是旋转几度
        current.transform.RotateAround(current.pivot.position, Vector3.forward, 90);
        foreach (Transform trans in current.transform)
        {
            if (trans.tag == "block")
            {
                //如果旋转后的物体里的子物体 有超出边界范围的 那么就先将旋转复原 然后返回false
                if (trans.position.y < 0 || trans.position.x < 0 || trans.position.x > 9)
                {
                    current.transform.RotateAround(current.pivot.position, Vector3.forward, -90);
                    return false;
                }
                GameObject go = blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)];
                //如果旋转后的位置 已经存在其他物体了 并且这个物体不是跟自己同一个父物体的 也复原+返回false
                if (go != null&&go.transform.parent!=trans.parent)
                {
                    current.transform.RotateAround(current.pivot.position, Vector3.forward, -90);
                    return false;
                }                 
            }          
        }
        //最后依旧复原 但返回true   至于为什么这里得先复原 看了 Turn函数会明白
        current.transform.RotateAround(current.pivot.position, Vector3.forward, -90);
        return true; 
    }
    void ToLeft()
    {
      
        if (CanToLeft())
        {
            AudioManager.Instance.PlaySound("Drop");
            //先遍历一遍 将当前的位置置空  再遍历第二遍，设置自己在二维数组里的新位置
            //我之前将置空与修改放在一次遍历里了 找到这个bug用的时间，够我写一遍这个案例的其余全部代码了
            //假设 0,1 0,2是一个物体下的  我们先将0,2置空 0,1设置成原先0,2位置的物体   再将0,1置空 0,0设置成原先0,1的物体 然而这里置空的0,1
            //有一定概率是前面我们将0,2置空设置的那个0,1  主要看子物体的顺序，  所以置空 与设置放在两个遍历里执行，才不会出现前面设置的被后面置空的情况
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                    blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;                 
                   // blocks[Mathf.RoundToInt(trans.position.x-1), Mathf.RoundToInt(trans.position.y)] = trans.gameObject;
                }
            }
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                  //  blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;
                    blocks[Mathf.RoundToInt(trans.position.x - 1), Mathf.RoundToInt(trans.position.y)] = trans.gameObject;
                }
            }
            //上面修改的是在数组里的位置， 这里修改的是显示的位置  只要修改一下父物体的Y就行了   
            //只修改父物体也是为了将我们上面用于旋转的那个点的位置一起移下来  要不然下次旋转就还按着原来的点旋转了
            Vector3 pos = current.transform.position;
            pos.x -= 1;
            current.transform.position = pos;
        }
    }
    void ToRight()
    {
        if (CanToRight())
        {
            AudioManager.Instance.PlaySound("Drop");
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                    blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;                 
                   // blocks[Mathf.RoundToInt(trans.position.x+1), Mathf.RoundToInt(trans.position.y)] = trans.gameObject;
                }
            }
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                  //  blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;
                    blocks[Mathf.RoundToInt(trans.position.x + 1), Mathf.RoundToInt(trans.position.y)] = trans.gameObject;
                }
            }
            Vector3 pos = current.transform.position;
            pos.x += 1;
            current.transform.position = pos;
        }
    }
    void Down()
    {
        if (CanDown())
        {
            AudioManager.Instance.PlaySound("Drop");
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                    blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;                  
                   // blocks[Mathf.RoundToInt(trans.position.x ), Mathf.RoundToInt(trans.position.y-1)] = trans.gameObject;
                }
            }
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                   // blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;
                    blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y - 1)] = trans.gameObject;
                }
            }
            Vector3 pos = current.transform.position;
            pos.y -= 1;
            current.transform.position = pos;
            
        }
        else
        {
            
            if (!Check())
            {
                AudioManager.Instance.PlaySound1("End");
            }
            
            current = CreateOneShape();
                    
        }
    }
    void Turn()
    {
        if (CanTurn())
        {
            AudioManager.Instance.PlaySound("Drop");
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                    blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = null;
                }
            }
            current.transform.RotateAround(current.pivot.position, Vector3.forward, 90);
            foreach (Transform trans in current.transform)
            {
                if (trans.tag == "block")
                {
                    blocks[Mathf.RoundToInt(trans.position.x), Mathf.RoundToInt(trans.position.y)] = trans.gameObject;
                }
            }
        }       

    }
    //重置游戏的逻辑  1分数归零  2状态设置为游戏中 3将原来的游戏物体删掉 4将数组清空 5生成第一个物体
    public void ResetGame()
    {
        score = 0;
        isGameStart = true;
        foreach (Transform item in shapeParent)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < 24; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                blocks[j, i]=null;
            }
        }
        current = CreateOneShape();      
    }
    //点击home按钮要做的事情  1删除物体 2清空数组 3分数归零（好像放到UI脚本里处理了）
    public void Home()
    {
        foreach (Transform item in shapeParent)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < 24; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                blocks[j, i] = null;
            }
        }
    }

    public void GameStop()
    {
        isGameStart = false;
    }
}