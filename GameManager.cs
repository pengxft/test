using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 消除元素相关的成员变量
    /// </summary>
    #region
    //消除元素的种类
    public enum iconType
    {
        EMPTY,
        NORMAL,
        BARRIER,
        ROW_CLEAR,
        COLUMN_CLEAR,
        SAMECOLOR,
        COUNT//标记类型
    }

    //图标预制体的字典，可以通过突变的种类来得到对应的图标游戏物体
    public Dictionary<iconType, GameObject> iconPrefabDict;

    [System.Serializable]
    public struct iconPrefab
    {
        public iconType type;
        public GameObject prefab;
    }

    public iconPrefab[] iconPrefabs;

    public GameObject gridPrefab;

    //元素数组
    private GameIcon[,] Icons;

    //要交换的两个对象
    private GameIcon pressedSweet;
    private GameIcon enteredSweet;

    #endregion

    //单例
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }

        set
        {
            _instance = value;
        }
    }

    private void Awake()
    {
        _instance = this;
        // DontDestroyOnLoad(this.gameObject);
    }

    //大网格的行列数
    public int xColumn;
    public int yRow;

    //填充时间
    public float fillTime;

    public float gameTime;
    public Slider slider;

    private bool gameOver;

    public int playerScore;//玩家得分

    public Text playerScoreText;//显示玩家得分

    private float addScoreTime;

    private float currentScore;

    public GameObject gameOverPanel;
    public Image img;

    public Text finalScoreText;

    public Text Finaldistance;

    public Text Tips;
    public Text GameResults;

    public Text StepText;   //计步器
    public int step;    //步数
    public GameObject But_Win;
    public GameObject But_Fail;
    public int x;//控制胜负分数


    string tip;

    //冬奥知识文本的随机调用方法
    public string tips()
    {
        int odg = Random.Range(1, 10);
        string tipsText;
        tipsText = File.ReadAllText(Application.streamingAssetsPath + "/内容"+odg+".txt");
        return tipsText;
    }
    public float distance()//计算滑行距离
    {
        float y = playerScore;
        float m = gameTime*3 + y * 2.7f;
        return m;
    }

    // Use this for initialization
    void Start()
    {

        tip = tips();
        //字典的实例化
        iconPrefabDict = new Dictionary<iconType, GameObject>();
        for (int i = 0; i < iconPrefabs.Length; i++)
        {
            if (!iconPrefabDict.ContainsKey(iconPrefabs[i].type))
            {
                iconPrefabDict.Add(iconPrefabs[i].type, iconPrefabs[i].prefab);
            }
        }


        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                GameObject iceCube = Instantiate(gridPrefab, CorrectPositon(x, y), Quaternion.identity);
                iceCube.transform.SetParent(transform);
            }
        }

        Icons = new GameIcon[xColumn, yRow];
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                CreateNewSweet(x, y, iconType.EMPTY);
            }
        }

        Destroy(Icons[7, 2].gameObject);
        CreateNewSweet(7, 2, iconType.BARRIER);

        Destroy(Icons[1, 0].gameObject);
        CreateNewSweet(1, 0, iconType.BARRIER);


        StartCoroutine(AllFill());
    }

    // Update is called once per frame
    void Update()
    {
        gameTime -= Time.deltaTime;
        slider.value = gameTime;
        //计时通关与否
        if (gameTime > 0 && playerScore >= x)
        {
            gameover();
            But_Win.SetActive(true);
            GameResults.text = "胜利！恭喜通关！";
        }
        else if(gameTime <= 0 && playerScore < x){
            gameover();
            But_Fail.SetActive(true);
            GameResults.text = "失败！再接再厉！";
        }
        //计步通关与否
        if(step > 0 && playerScore >= x){
            gameover();
            But_Win.SetActive(true);
            GameResults.text = "胜利！恭喜通关！";
        }
        else if(step <= 0){
            gameover();
            But_Fail.SetActive(true);
            GameResults.text = "失败！再接再厉！";
        }


        // timeText.text = gameTime.ToString("0");
        playerScoreText.text = playerScore.ToString();
        //监听是否选择了要交换的obj和鼠标左键是否按下
        if(pressedSweet != null && Input.GetMouseButtonDown(0)){
            //检测鼠标是否放在UI上
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() == false)
            {
                //鼠标转换成射线
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //射线
                RaycastHit hit;
                //从摄像机发射一条射线，射线的范围是1000米，只和IsCollider层发生碰撞，碰撞后得到碰撞体的信息，
                //并返回一个布尔值。
                bool isCollider = Physics.Raycast(ray, out hit, 1000, LayerMask.GetMask("IsCollider"));
                if(isCollider){
                    step = step - 1;
                    StepText.text = step.ToString();
                }
            }
        }
    }

    public void gameover()
    {
        gameTime = 0;
        gameOverPanel.SetActive(true);
        img.material = null;
        Finaldistance.text = distance().ToString() + "m";
        finalScoreText.text = playerScore.ToString();
        Tips.text = tip;
        gameOver = true;
    }

    public Vector3 CorrectPositon(int x, int y)
    {
        //实际需要实例化巧克力块的X位置=GameManager位置的X坐标-大网格长度的一半+行列对应的X坐标
        //实际需要实例化巧克力块的Y位置=GameManager位置的Y坐标+大网格高度的一半-行列对应的Y坐标
        return new Vector3(transform.position.x - xColumn / 2f + x, transform.position.y + yRow / 2f - y);
    }

    //产生消除元素的方法
    public GameIcon CreateNewSweet(int x, int y, iconType type)
    {
        GameObject newSweet = Instantiate(iconPrefabDict[type], CorrectPositon(x, y), Quaternion.identity);
        newSweet.transform.parent = transform;

        Icons[x, y] = newSweet.GetComponent<GameIcon>();
        Icons[x, y].Init(x, y, this, type);

        return Icons[x, y];
    }

    //全部填充的方法
    public IEnumerator AllFill()
    {
        bool needRefill = true;

        while (needRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (Fill())
            {
                yield return new WaitForSeconds(fillTime);
            }

            //清除所有我们已经匹配好的元素
            needRefill = ClearAllMatchedIcon();
        }


    }

    //分步填充
    public bool Fill()
    {
        bool filledNotFinished = false;//判断本次填充是否完成

        for (int y = yRow - 2; y >= 0; y--)
        {
            for (int x = 0; x < xColumn; x++)
            {
                GameIcon Icon = Icons[x, y];//得到当前元素位置的对象

                if (Icon.CanMove())//如果无法移动，则无法往下填充 
                {
                    GameIcon sweetBelow = Icons[x, y + 1];

                    if (sweetBelow.Type == iconType.EMPTY)//垂直填充
                    {
                        Destroy(sweetBelow.gameObject);
                        Icon.MovedComponent.Move(x, y + 1, fillTime);
                        Icons[x, y + 1] = Icon;
                        CreateNewSweet(x, y, iconType.EMPTY);
                        filledNotFinished = true;
                    }
                    else         //斜向填充
                    {
                        for (int down = -1; down <= 1; down++)
                        {
                            if (down != 0)
                            {
                                int downX = x + down;

                                if (downX >= 0 && downX < xColumn)
                                {
                                    GameIcon downIcon = Icons[downX, y + 1];

                                    if (downIcon.Type == iconType.EMPTY)
                                    {
                                        bool canfill = true;//用来判断垂直填充是否可以满足填充要求

                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            GameIcon IconAbove = Icons[downX, aboveY];
                                            if (IconAbove.CanMove())
                                            {
                                                break;
                                            }
                                            else if (!IconAbove.CanMove() && IconAbove.Type != iconType.EMPTY)
                                            {
                                                canfill = false;
                                                break;
                                            }
                                        }

                                        if (!canfill)
                                        {
                                            Destroy(downIcon.gameObject);
                                            Icon.MovedComponent.Move(downX, y + 1, fillTime);
                                            Icons[downX, y + 1] = Icon;
                                            CreateNewSweet(x, y, iconType.EMPTY);
                                            filledNotFinished = true;
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }

            }
        }

        //最上排的特殊情况
        for (int x = 0; x < xColumn; x++)
        {
            GameIcon Icon = Icons[x, 0];

            if (Icon.Type == iconType.EMPTY)
            {
                GameObject newSweet = Instantiate(iconPrefabDict[iconType.NORMAL], CorrectPositon(x, -1), Quaternion.identity);
                newSweet.transform.parent = transform;

                Icons[x, 0] = newSweet.GetComponent<GameIcon>();
                Icons[x, 0].Init(x, -1, this, iconType.NORMAL);
                Icons[x, 0].MovedComponent.Move(x, 0, fillTime);
                Icons[x, 0].ColoredComponent.SetColor((ColorIcon.ColorType)Random.Range(0, Icons[x, 0].ColoredComponent.NumColors));
                filledNotFinished = true;
            }
        }

        return filledNotFinished;
    }

    //元素是否相邻的判断方法
    private bool IsFriend(GameIcon Icon1, GameIcon Icon2)
    {
        return (Icon1.X == Icon2.X && Mathf.Abs(Icon1.Y - Icon2.Y) == 1) ||
                 (Icon1.Y == Icon2.Y && Mathf.Abs(Icon1.X - Icon2.X) == 1);
    }

    //交换两个元素的方法
    private void ExchangeSweets(GameIcon Icon1, GameIcon Icon2)
    {
        if (Icon1.CanMove() && Icon2.CanMove())
        {
            Icons[Icon1.X, Icon1.Y] = Icon2;
            Icons[Icon2.X, Icon2.Y] = Icon1;

            if (MatchIcons(Icon1, Icon2.X, Icon2.Y) != null || MatchIcons(Icon2, Icon1.X, Icon1.Y) != null || 
                    Icon1.Type == iconType.SAMECOLOR ||Icon2.Type == iconType.SAMECOLOR)
            {

                int tempX = Icon1.X;
                int tempY = Icon1.Y;

                Icon1.MovedComponent.Move(Icon2.X, Icon2.Y, fillTime);
                Icon2.MovedComponent.Move(tempX, tempY, fillTime);

                if(Icon1.Type == iconType.SAMECOLOR && Icon1.CanClear() && Icon2.CanClear()){
                    ClearColorAny clearColor = Icon1.GetComponent<ClearColorAny>();
                    if(clearColor != null){
                        clearColor.ClearColor = Icon2.ColoredComponent.Color;
                    }
                    ClearSweet(Icon1.X,Icon1.Y);
                }

                if(Icon2.Type == iconType.SAMECOLOR && Icon2.CanClear() && Icon1.CanClear()){
                    ClearColorAny clearColor = Icon2.GetComponent<ClearColorAny>();
                    if(clearColor != null){
                        clearColor.ClearColor = Icon1.ColoredComponent.Color;
                    }
                    ClearSweet(Icon2.X,Icon2.Y);
                }
                ClearAllMatchedIcon();
                StartCoroutine(AllFill());
                //为防止鼠标点击速度过快导致交换物体被拖拽回去，需要把它们置空
                pressedSweet = null;
                enteredSweet = null;
            }
            else
            {
                Icons[Icon1.X, Icon1.Y] = Icon1;
                Icons[Icon2.X, Icon2.Y] = Icon2;
            }

        }
    }

    /// <summary>
    /// 玩家对消除元素操作进行拖拽处理的方法
    /// </summary>
    #region
    public void PressIcon(GameIcon Icon)
    {
        if (gameOver)
        {
            return;
        }
        pressedSweet = Icon;
    }

    public void EnterIcon(GameIcon Icon)
    {
        if (gameOver)
        {
            return;
        }
        enteredSweet = Icon;
    }

    public void ReleaseIcon()
    {
        if (gameOver)
        {
            return;
        }
        if (IsFriend(pressedSweet, enteredSweet))
        {
            ExchangeSweets(pressedSweet, enteredSweet);
        }
        
    }
    #endregion

    /// <summary>
    /// 清除匹配的方法
    /// </summary>
    #region
    //匹配方法
    public List<GameIcon> MatchIcons(GameIcon Icon, int newX, int newY)
    {
        if (Icon.CanColor())
        {
            ColorIcon.ColorType color = Icon.ColoredComponent.Color;
            List<GameIcon> matchRowIcons = new List<GameIcon>();
            List<GameIcon> matchLineIcons = new List<GameIcon>();
            List<GameIcon> finishedMatchingIcons = new List<GameIcon>();

            //行匹配
            matchRowIcons.Add(Icon);

            //i=0代表往左，i=1代表往右
            for (int i = 0; i <= 1; i++)
            {
                for (int xDistance = 1; xDistance < xColumn; xDistance++)
                {
                    int x;
                    if (i == 0)
                    {
                        x = newX - xDistance;
                    }
                    else
                    {
                        x = newX + xDistance;
                    }
                    if (x < 0 || x >= xColumn)
                    {
                        break;
                    }

                    if (Icons[x, newY].CanColor() && Icons[x, newY].ColoredComponent.Color == color)
                    {
                        matchRowIcons.Add(Icons[x, newY]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchRowIcons.Count >= 3)
            {
                for (int i = 0; i < matchRowIcons.Count; i++)
                {
                    finishedMatchingIcons.Add(matchRowIcons[i]);
                }
            }

            //L T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchRowIcons.Count >= 3)
            {
                for (int i = 0; i < matchRowIcons.Count; i++)
                {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行列遍历
                    // 0代表上方 1代表下方
                    for (int j = 0; j <= 1; j++)
                    {
                        for (int yDistance = 1; yDistance < yRow; yDistance++)
                        {
                            int y;
                            if (j == 0)
                            {
                                y = newY - yDistance;
                            }
                            else
                            {
                                y = newY + yDistance;
                            }
                            if (y < 0 || y >= yRow)
                            {
                                break;
                            }

                            if (Icons[matchRowIcons[i].X, y].CanColor() && Icons[matchRowIcons[i].X, y].ColoredComponent.Color == color)
                            {
                                matchLineIcons.Add(Icons[matchRowIcons[i].X, y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchLineIcons.Count < 2)
                    {
                        matchLineIcons.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchLineIcons.Count; j++)
                        {
                            finishedMatchingIcons.Add(matchLineIcons[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingIcons.Count >= 3)
            {
                return finishedMatchingIcons;
            }

            matchRowIcons.Clear();
            matchLineIcons.Clear();

            matchLineIcons.Add(Icon);

            //列匹配

            //i=0代表往上，i=1代表往下
            for (int i = 0; i <= 1; i++)
            {
                for (int yDistance = 1; yDistance < yRow; yDistance++)
                {
                    int y;
                    if (i == 0)
                    {
                        y = newY - yDistance;
                    }
                    else
                    {
                        y = newY + yDistance;
                    }
                    if (y < 0 || y >= yRow)
                    {
                        break;
                    }

                    if (Icons[newX, y].CanColor() && Icons[newX, y].ColoredComponent.Color == color)
                    {
                        matchLineIcons.Add(Icons[newX, y]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchLineIcons.Count >= 3)
            {
                for (int i = 0; i < matchLineIcons.Count; i++)
                {
                    finishedMatchingIcons.Add(matchLineIcons[i]);
                }
            }

            //L T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchLineIcons.Count >= 3)
            {
                for (int i = 0; i < matchLineIcons.Count; i++)
                {
                    //列匹配列表中满足匹配条件的每个元素左右依次进行列遍历
                    // 0代表左方 1代表右方
                    for (int j = 0; j <= 1; j++)
                    {
                        for (int xDistance = 1; xDistance < xColumn; xDistance++)
                        {
                            int x;
                            if (j == 0)
                            {
                                x = newY - xDistance;
                            }
                            else
                            {
                                x = newY + xDistance;
                            }
                            if (x < 0 || x >= xColumn)
                            {
                                break;
                            }

                            if (Icons[x, matchLineIcons[i].Y].CanColor() && Icons[x, matchLineIcons[i].Y].ColoredComponent.Color == color)
                            {
                                matchRowIcons.Add(Icons[x, matchLineIcons[i].Y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchRowIcons.Count < 2)
                    {
                        matchRowIcons.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchRowIcons.Count; j++)
                        {
                            finishedMatchingIcons.Add(matchRowIcons[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingIcons.Count >= 3)
            {
                return finishedMatchingIcons;
            }
        }

        return null;
    }

    //清除方法
    public bool ClearSweet(int x, int y)
    {
        if (Icons[x, y].CanClear() && !Icons[x, y].ClearedComponent.IsClearing)
        {
            Icons[x, y].ClearedComponent.Clear();
            CreateNewSweet(x, y, iconType.EMPTY);
            ClearBarrier(x,y);
            return true;
        }
        return false;
    }

    //清除障碍物的方法
    //在清除普通元素时遍历周围元素是否为障碍物，如果是，就销毁
    private void ClearBarrier(int x,int y){    //清除障碍物需要知道当前消除物体的位置,坐标是当前被消除的物体的坐标
        //左右遍历
        for(int friendX = x-1;friendX <= x+1; friendX++){  //x-1为当前位置左边，x+1为当前位置右边
            if(friendX != x && friendX >=0 && friendX < xColumn){   //排除边界和自身X坐标的情况
                if(Icons[friendX,y].Type == iconType.BARRIER && Icons[friendX,y].CanClear()){     //得到自身是否为障碍物且是否能被清除
                    Icons[friendX,y].ClearedComponent.Clear();
                    CreateNewSweet(friendX,y,iconType.EMPTY);
                }
            }
        }
        //上下遍历
        for (int friendY = y-1;friendY <= y+1;friendY++){
            if(friendY != y && friendY >= 0 && friendY < yRow){
                if(Icons[x,friendY].Type == iconType.BARRIER && Icons[x,friendY].CanClear()){
                    Icons[x,friendY].ClearedComponent.Clear();
                    CreateNewSweet(x,friendY,iconType.EMPTY);
                }
            }
        }
    }

    //清除全部完成匹配的元素
    public bool ClearAllMatchedIcon()
    {
        bool needRedfill = false;

        for (int y = 0; y < yRow; y++)
        {
            for (int x = 0; x < xColumn; x++)
            {
                if (Icons[x,y]&&Icons[x, y].CanClear())
                {
                    List<GameIcon> matchList = MatchIcons(Icons[x, y], x, y);

                    if (matchList != null)
                    {
                        //想要产生行列消除的元素，就要在完成清除之前产生
                        iconType specialIconsType = iconType.COUNT;    //判断是否产生特殊元素(行、列、同色)
                        GameIcon randomIcon = matchList[Random.Range(0, matchList.Count)];    //随机产生的位置
                        int specialIconX = randomIcon.X;
                        int specialIconY = randomIcon.Y;

                        //判断是否一次消除了4个元素，如果是，则生成特殊元素
                        if(matchList.Count == 4)
                        {
                            //Random.Range接收int型数据，所以首先枚举类型转换为int类型
                            //specialSweetsType接收枚举类型，再将Random.Range接收到的int型数据强制转化为SweetsType类型
                            specialIconsType = (iconType)Random.Range((int)iconType.ROW_CLEAR,(int)iconType.SAMECOLOR);
                        }
                        //如果同时消除5个元素，产生同色消除
                        else if(matchList.Count >= 5){
                            specialIconsType = iconType.SAMECOLOR;
                        }
                        

                        for (int i = 0; i < matchList.Count; i++)
                        {
                            if (ClearSweet(matchList[i].X, matchList[i].Y))
                            {
                                needRedfill = true;
                            }
                        }

                        if(specialIconsType != iconType.COUNT)   //如果当前的特殊元素不等于标记类型
                        {
                            Destroy(Icons[specialIconX,specialIconY]);
                            GameIcon newSweet = CreateNewSweet(specialIconX, specialIconY, specialIconsType);
                            //判断是否为行消除或者列消除  并且可以对新生成的数组里的第一个元素进行着色
                            if(specialIconsType == iconType.ROW_CLEAR||specialIconsType == iconType.COLUMN_CLEAR && newSweet.CanColor() && matchList[0].CanColor())
                            {
                                newSweet.ColoredComponent.SetColor(matchList[0].ColoredComponent.Color);
                            }
                            //加上同色消除的特殊元素类型
                            else if(specialIconsType == iconType.SAMECOLOR && newSweet.CanColor()){
                                newSweet.ColoredComponent.SetColor(ColorIcon.ColorType.ANY);
                            }
                        }
                    }
                }
            }
        }
        return needRedfill;
    }

    #endregion

    //清除行的方法
    public void ClearRow(int row)
    {
        for(int x = 0; x < xColumn; x++)
        {
            ClearSweet(x, row);
            GameObject.Find("New_Text").SendMessage("Playm_anim");
        }
    }
    //清除列的方法
    public void ClearColomn(int colomn)
    {
        for(int y = 0; y < yRow; y++)
        {
            ClearSweet(colomn, y);
            GameObject.Find("New_Text").SendMessage("Playm_anim");
        }
    }
    //同色消除的方法
    public void ClearColor(ColorIcon.ColorType color){
        for(int x = 0; x < xColumn; x++){
            for(int y = 0; y < yRow; y++){
                if(Icons[x,y].CanColor() && (Icons[x,y].ColoredComponent.Color == color || color == ColorIcon.ColorType.ANY)){
                    ClearSweet(x,y);
                    GameObject.Find("New_Text").SendMessage("Playm_anim");
                }
            }
        }
    }


    public void ReturnToMain()//返回主界面
    {
        SceneManager.LoadScene(0);
    }

    public string SceneName;
    public void Replay1()//返回游戏界面
    {
        SceneManager.LoadScene(1);
    }
    public void Replay2(){
        SceneManager.LoadScene(4);
    }
    public void NextScene(){
        SceneManager.LoadScene(SceneName);
    }
}
