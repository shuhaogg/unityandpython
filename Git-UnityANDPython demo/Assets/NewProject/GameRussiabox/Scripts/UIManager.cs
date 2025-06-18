
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : MonoSingleton<UIManager>
{
    //各个panel的引用
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject gameoverPanle;
    public GameObject rankPanel;
    public GameObject settingPanel;

    //分数
    public Text score;
    public Text highest;
    public Text gameoverScore;
    //排行榜里的分数
    public Text highest1;
    public Text highest2;
    public Text highest3;
    //静音 以及 暂停按钮的引用 因为要修改他的图片 所以这里存一下比较方便
    public GameObject muteBtn;
    public GameObject pauseBtn;
    public GameObject exitBtn;

    bool isPause;
    GameObject currentPanel;
    /*
    private void Update()
    {
        //如果按下右键  就把当前的界面隐藏   当前只有排行榜与设置界面 可以设置为当前界面  因为其他界面 逻辑上是不允许靠右键关闭的
        if (Input.GetMouseButtonDown(1))
        {
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                currentPanel = null;
            }
        }
    }
    */
    //加分
    public void AddScore(int score)
    {
        GameManager3.Instance.score += score;
        this.score.text = GameManager3.Instance.score.ToString();
    }
    //更新最高分
    public void UpdateHighest(int highest)
    {
        this.highest.text = highest.ToString();
    }

    void UpdateRank()
    {
        highest1.text = PlayerPrefs.GetInt("highest1").ToString();
        highest2.text = PlayerPrefs.GetInt("highest2").ToString();
        highest3.text = PlayerPrefs.GetInt("highest3").ToString();
    }
    //主菜单里的 开始按钮
    public void StartGameBtn()
    {
        UpdateHighest(PlayerPrefs.GetInt("highest1"));
        rankPanel.SetActive(false);
        settingPanel.SetActive(false);
        AudioManager.Instance.PlaySound("Cursor_002");
        menuPanel.transform.DOScale(Vector3.one * 1.5f, 0.2f);
        GameManager3.Instance.timer.AddTimeTask((a) => { menuPanel.SetActive(false); gamePanel.SetActive(true); }, 200, PETimeUnit.Millisecond);
        GameManager3.Instance.timer.AddTimeTask((a) =>
        {
            GameManager3.Instance.current = GameManager3.Instance.CreateOneShape();
            GameManager3.Instance.isGameStart = true;
        }, 700, PETimeUnit.Millisecond);


    }
    //主菜单里的 排行榜按钮
    public void RankBtn()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        if (rankPanel.activeSelf)
        {
            rankPanel.SetActive(false);
            currentPanel = null;
            return;
        }
        UpdateRank();
        currentPanel = rankPanel;
        rankPanel.SetActive(true);
        settingPanel.SetActive(false);
    }
    //主菜单里的 设置按钮
    public void SettingBtn()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        //如果这个界面已经显示  那么再次点击设置按钮 我们应该将设置界面关掉
        if (settingPanel.activeSelf)
        {
            settingPanel.SetActive(false);
            currentPanel = null;
            return;
        }
        currentPanel = settingPanel;
        rankPanel.SetActive(false);
        settingPanel.SetActive(true);
    }
    //游戏界面的 暂停按钮
    public void PauseBtn()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        if (isPause)
        {
            isPause = false;
            Time.timeScale = 1;//通过这个属性 暂停游戏
            pauseBtn.transform.Find("pause").gameObject.SetActive(true);
            pauseBtn.transform.Find("resume").gameObject.SetActive(false);
        }
        else
        {
            isPause = true;
            Time.timeScale = 0;
            pauseBtn.transform.Find("pause").gameObject.SetActive(false);
            pauseBtn.transform.Find("resume").gameObject.SetActive(true);
        }



    }
    //游戏结束界面的 重置游戏按钮
    public void ReStartBtn()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        gameoverPanle.SetActive(false);
        gamePanel.SetActive(true);
        GameManager3.Instance.ResetGame();
        score.text = "0";
    }
    //游戏结束界面的 home按钮
    public void Home()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        gameoverPanle.SetActive(false);
        gamePanel.SetActive(false);
        menuPanel.SetActive(true);

        GameManager3.Instance.Home();
        GameManager3.Instance.score = 0;
        menuPanel.transform.DOScale(Vector3.one, 0.2f);
        score.text = "0";
    }
    //清除本地分数记录 并且刷新排行榜
    public void ClearHighest()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        PlayerPrefs.SetInt("highest1", 0);
        PlayerPrefs.SetInt("highest2", 0);
        PlayerPrefs.SetInt("highest3", 0);
        UpdateRank();
    }
    //设置界面的 静音按钮
    public void MuteBtn()
    {
        AudioManager.Instance.PlaySound("Cursor_002");
        AudioManager.Instance.Mute();
        //找到button下面的mute图片 如果是显示着的 就隐藏  反之   
        muteBtn.transform.Find("mute").gameObject.SetActive(!muteBtn.transform.Find("mute").gameObject.activeSelf);
    }

    public void StopBtn()
    {

        GameManager3.Instance.GameStop();
        if (isPause)
        {
            isPause = false;
            Time.timeScale = 0;
            pauseBtn.transform.Find("pause").gameObject.SetActive(true);
            pauseBtn.transform.Find("resume").gameObject.SetActive(false);
        }


        AudioManager.Instance.PlaySound("Cursor_002");
        //gameoverPanle.SetActive(false);
        gamePanel.SetActive(false);
        menuPanel.SetActive(true);

        Time.timeScale = 1;

        GameManager3.Instance.Home();
        GameManager3.Instance.score = 0;
        menuPanel.transform.DOScale(Vector3.one, 0.2f);
        score.text = "0";
    }
}
