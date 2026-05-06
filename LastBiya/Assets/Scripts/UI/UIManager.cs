using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    public static UIManager instance;
    public Dictionary<string, string> pathDic;
    public Dictionary<string, GameObject> perfabDic;
    public Dictionary<string, BasePanel> panelDic;

    private Transform uiRoot;

    public Transform UiRoot
    {
        get
        {
            if (uiRoot == null)
            {
                uiRoot = GameObject.Find("Canvas").transform;
            }
            return uiRoot;
        }
    }

    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UIManager();
            }
            return instance;
        }
    }

    private UIManager()
    {
        InitDicts();
    }

    void InitDicts()
    {
        perfabDic = new Dictionary<string, GameObject>();
        panelDic = new Dictionary<string, BasePanel>();
        pathDic = new Dictionary<string, string>()
        {
            { UIConst.mainMenuPanel, "Menu/MainMenuPanel" },
            { UIConst.HUDPanel, "Menu/HUDPanel" },
            { UIConst.inventoryPanel, "Menu/InventoryPanel" },
            { UIConst.dialoguePanel, "Menu/DialoguePanel" },
        };
    }

    public BasePanel OpenPanel(string name)
    {
        BasePanel panel = null;

        // 已存在：直接复用，显示出来
        if (panelDic.TryGetValue(name, out panel))
        {
            panel.OpenPanel(name);
            return panel;
        }

        // 检查路径
        string path = "";
        if (!pathDic.TryGetValue(name, out path))
        {
            Debug.LogWarning("未找到面板路径：" + name);
            return null;
        }

        // 加载预制体（缓存）
        GameObject panelPrefab = null;
        if (!perfabDic.TryGetValue(name, out panelPrefab))
        {
            string realPath = "Perfab/Panel/" + path;
            panelPrefab = Resources.Load<GameObject>(realPath) as GameObject;
            perfabDic.Add(name, panelPrefab);
        }

        // 首次创建
        GameObject panelObject = GameObject.Instantiate(panelPrefab, UiRoot, false);
        panel = panelObject.GetComponent<BasePanel>();
        panel.OpenPanel(name);
        panelDic.Add(name, panel);
        return panel;
    }

    public bool ClosePanel(string name)
    {
        BasePanel panel = null;
        if (!panelDic.TryGetValue(name, out panel))
        {
            Debug.LogWarning("面板未打开：" + name);
            return false;
        }

        panel.ClosePanel();
        return true;
    }
}

public class UIConst
{
    public const string mainMenuPanel = "MineMenuPanel";
    public const string HUDPanel = "HUDPanel";
    public const string inventoryPanel = "InventoryPanel";
    public const string dialoguePanel = "DialoguePanel";
}
