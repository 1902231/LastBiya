using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    protected new string name;

    public virtual void OpenPanel(string name)
    {
        this.name = name;
        this.gameObject.SetActive(true);
    }

    public virtual void ClosePanel()
    {
        this.gameObject.SetActive(false);
        // 不销毁，不从字典移除，下次 OpenPanel 时复用
    }
}
