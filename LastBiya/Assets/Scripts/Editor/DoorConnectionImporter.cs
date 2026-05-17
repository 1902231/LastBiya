using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

public class DoorConnectionImporter : EditorWindow
{
    private string csvPath = "Assets/Data/DoorConnections.csv";
    private string tablePath = "Assets/Data So/DoorConnectionTable.asset";
    private DoorConnectionsTableSO targetTable;

    [MenuItem("Tools/Import Door Connections")]
    public static void ShowWindow()
    {
        GetWindow<DoorConnectionImporter>("门连接导工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV门连接导入工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //CSV文件路径
        EditorGUILayout.LabelField("CSV文件路径：");
        csvPath = EditorGUILayout.TextField(csvPath);

        if (GUILayout.Button("选择CSV文件"))
        {
            string path = EditorUtility.OpenFilePanel("选择CSV文件", "Assets/Data", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvPath = "Assets" + path.Replace(Application.dataPath, "");
            }
        }

        EditorGUILayout.Space();

        //目标so路径
        EditorGUILayout.LabelField("目标DoorConnectionTableSO:");
        targetTable = EditorGUILayout.ObjectField(targetTable, typeof(DoorConnectionsTableSO), false) as DoorConnectionsTableSO;

        EditorGUILayout.Space();

        if (targetTable != null)
        {
            EditorGUILayout.TextField($"当前的连接数：{targetTable.connections.Count}");
        }
        EditorGUILayout.Space();


        //导入按钮
        if (GUILayout.Button("导入CSV", GUILayout.Height(30)))
        {
            ImportCSV();
        }

        EditorGUILayout.Space();

        if (targetTable == null && GUILayout.Button("创建新的DoorConnectionTableSO"))
        {
            targetTable = CreateInstance<DoorConnectionsTableSO>();
            AssetDatabase.CreateAsset(targetTable, "Assets/Data So/DoorConnectionTable.asset");
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.Space();

        //验证按钮
        if (targetTable != null && GUILayout.Button("验证连接表"))
        {
            targetTable.ValidateConnections();
        }

        //清空按钮
        if (targetTable != null && GUILayout.Button("清空连接表"))
        {
            ClearConnections();
        }

        if (targetTable != null && GUILayout.Button("导出CSV"))
        {
            ExportCSV();
        }

    }

    void ImportCSV()
    {
        //检查目标CSV文件是否存在
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("错误", $"CSV文件不存在：{csvPath}", "确定");
            return;
        }

        //检查或创建目标so
        if (targetTable == null)
        {
            targetTable = AssetDatabase.LoadAssetAtPath<DoorConnectionsTableSO>(tablePath);
            if (targetTable == null)
            {
                targetTable = CreateInstance<DoorConnectionsTableSO>();
                AssetDatabase.CreateAsset(targetTable, tablePath);
                Debug.Log($"创建新的DoorConnectionTable：{tablePath}");
            }
        }

        //读取CSV
        string[] lines = File.ReadAllLines(csvPath);
        List<DoorConnection> connections = new List<DoorConnection>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            //跳过空行和注释
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            //分割CSV行
            string[] value = line.Split(',');

            //验证列数
            if (value.Length != 4)
            {
                Debug.LogWarning($"第 {i + 1} 行格式错误，跳过：{line}");
                continue;
            }

            //汲取数据
            string formRoomID = value[0].Trim();
            string formDoorID = value[1].Trim();
            string toRoomID = value[2].Trim();
            string todoorID = value[3].Trim();

            //创建连接对象
            connections.Add(new DoorConnection(formRoomID, formDoorID, toRoomID, todoorID));

        }

        //更新so
        targetTable.connections = connections;
        EditorUtility.SetDirty(targetTable);
        AssetDatabase.SaveAssets();

        AutoConfigureRoomDoors(connections);

        Debug.Log($"导入成功！共导入 {connections.Count} 个门连接");
        EditorUtility.DisplayDialog("导入成功", $"成功导入 {connections.Count} 个门连接", "确定");

        // 自动验证
        targetTable.ValidateConnections();
    }
    private void ExportCSV()
    {
        if (targetTable == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择DoorConnectionTable", "确定");
            return;
        }

        string path = EditorUtility.SaveFilePanel("导出CSV", "Assets/Data", "DoorConnections", "csv");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        List<string> lines = new List<string>();
        lines.Add("RoomID,DoorID,TargetRoomID,TargetDoorID");

        foreach (var connection in targetTable.connections)
        {
            string line = $"{connection.fromRoomID},{connection.fromDoorID},{connection.toRoomID},{connection.toDoorID}";
            lines.Add(line);
        }

        File.WriteAllLines(path, lines);

        Debug.Log($"导出成功！文件路径：{path}");
        EditorUtility.DisplayDialog("导出成功", $"成功导出 {targetTable.connections.Count} 个连接", "确定");

    } 

    private void ClearConnections()
    {
        targetTable.connections.Clear();
        EditorUtility.SetDirty (targetTable);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("清空成功","已清空全部门连接", "确定");
    }

    /// <summary>
    /// 自动配置所有房间的门列表
    /// </summary>
    private void AutoConfigureRoomDoors(List<DoorConnection> connections)
    {
        // 1. 收集所有房间的门信息
        Dictionary<string, HashSet<string>> roomDoors = new Dictionary<string, HashSet<string>>();

        foreach (var connection in connections)
        {
            // 收集来源房间的门
            if (!roomDoors.ContainsKey(connection.fromRoomID))
            {
                roomDoors[connection.fromRoomID] = new HashSet<string>();
            }
            roomDoors[connection.fromRoomID].Add(connection.fromDoorID);

            // 收集目标房间的门
            if (!roomDoors.ContainsKey(connection.toRoomID))
            {
                roomDoors[connection.toRoomID] = new HashSet<string>();
            }
            roomDoors[connection.toRoomID].Add(connection.toDoorID);
        }

        // 2. 查找所有 GameSceneSO 资源
        string[] guids = AssetDatabase.FindAssets("t:GameSceneSO");
        int configuredCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameSceneSO room = AssetDatabase.LoadAssetAtPath<GameSceneSO>(path);

            if (room == null || string.IsNullOrEmpty(room.roomID))
                continue;

            // 3. 如果这个房间在CSV中有门，自动配置
            if (roomDoors.ContainsKey(room.roomID))
            {
                List<DoorData> doors = new List<DoorData>();

                foreach (string doorID in roomDoors[room.roomID])
                {
                    // 根据门ID推断方向
                    DoorDirection direction = InferDoorDirection(doorID);

                    doors.Add(new DoorData(doorID, direction));
                }

                room.doors = doors;
                EditorUtility.SetDirty(room);
                configuredCount++;

                Debug.Log($"自动配置房间 {room.roomID} 的 {doors.Count} 个门");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"共自动配置了 {configuredCount} 个房间的门列表");
    }

    /// <summary>
    /// 根据门ID推断方向（约定：门ID格式为 Direction_Number）
    /// </summary>
    private DoorDirection InferDoorDirection(string doorID)
    {
        string lowerID = doorID.ToLower();

        if (lowerID.Contains("left"))
            return DoorDirection.Left;
        else if (lowerID.Contains("right"))
            return DoorDirection.Right;
        else if (lowerID.Contains("top") || lowerID.Contains("up"))
            return DoorDirection.Top;
        else if (lowerID.Contains("bottom") || lowerID.Contains("down"))
            return DoorDirection.Bottom;

        Debug.LogWarning($"无法从门ID '{doorID}' 推断方向，默认使用 Right");
        return DoorDirection.Right;
    }

}
