#if TOOLS
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace SceneCore_Space
{
    [Tool]
    public partial class SceneCore : EditorPlugin
    {
        //主界面
        private Control MainPanelInstance;//主节目
        private Tree tree;//树
        private PopupMenu popupMenu;//菜单

        //维护json文件
        SaveLoadData saveLoadData;
        //场景树
        public SceneLable labledata;

        public override void _EnterTree()
        {
            if (OS.HasFeature("debug"))
            {
                MainPanelInstance = GD.Load<PackedScene>("res://addons/SceneView/scene/SceneView.tscn").Instantiate<Control>();
                tree = MainPanelInstance.GetNode<Tree>("Tree");
                popupMenu = MainPanelInstance.GetNode<PopupMenu>("PopupMenu");
                popupMenu.Hide();//隐藏
                popupMenu.Connect(PopupMenu.SignalName.IdPressed, new Callable(this, MethodName.OnMenu));

                tree.Connect(Tree.SignalName.ItemMouseSelected, new Callable(this, MethodName.ItemMouseSelected));
                tree.ItemEdited += OnTreeItemEdited;

                saveLoadData = new SaveLoadData();
                labledata = saveLoadData.GetSceneLabelList();
				sceneLable.SceneDataChanged += (sender, e) =>//委托变化事件
				{
					saveLoadData.SaveData();
				};
                //saveLoadData.SaveData();

                SceneLable other_lable = new SceneLable("/other");
                labledata.AddLabel(other_lable);//默认加--加在数据里

                ////////////////测试数据

                IniData();
                IniView(false);

                AddControlToDock(DockSlot.LeftUl, MainPanelInstance);
            }

            //注册 自动加载
            //AddAutoloadSingleton(AutoloadName, "res://addons/SceneView/scene/SceneView.tscn");
        }

        public override void _ExitTree()
        {
            //反注册 自动加载
            //RemoveAutoloadSingleton(AutoloadName);
            if (OS.HasFeature("debug") && MainPanelInstance != null)
            {
                RemoveControlFromDocks(MainPanelInstance);
                MainPanelInstance.Free();
            }
        }

        public void DropData()
        {

        }


        /// <summary>
        ///鼠标选中某选项
        /// </summary>
        public void ItemMouseSelected(Vector2 position, int mouse_button_index)
        {
            if (mouse_button_index == 2)//鼠标左键
            {
                SetShow(true, position);//显示菜单
            }
        }

        /// <summary>
        ///设置选项菜单是否显示
        /// </summary>
        public void SetShow(bool IsShow, Vector2 position)
        {
            if (IsShow)
            {
                TreeItem treeitem = tree.GetSelected();//当前选中
                if (treeitem != null)
                {
                    string type = (string)treeitem.GetMetadata(0);
                    if (type.Equals("scene"))
                    {
                        //GD.Print("是场景");
                    }
                    else if (type.Equals("lable"))
                    {
                        //GD.Print("是标签");
                    }
                    popupMenu.Position = new Vector2I((int)position.X + 14, (int)(position.Y) + 100);
                    popupMenu.Show();
                }
            }
            else
                popupMenu.Hide();
        }

        /// <summary>
        ///选项菜单按钮
        /// </summary>
        public void OnMenu(int id)
        {
            if (id == 0)
            {//添加标签
                AddLable();
            }
            else if (id == 1)
            {
                EditLableName();
            }
        }

        /// <summary>
        ///新的修改项
        /// </summary>
        public void OnTreeItemEdited()
        {
            TreeItem treeitem = tree.GetEdited();
            if (treeitem != null)
            {
                List<SceneLable> list_able = labledata.GetAllSceneLabel();//获取所有标签
                string value = (string)treeitem.GetMetadata(1);
                SceneLable scenelable = labledata.QueryLable(list_able, value);//查询对应标签
                if (scenelable != null)//文件数据中存在该标签
                {
                    scenelable.lable_name = scenelable.parent_lable_name + "/" + treeitem.GetText(0);//给标签下，加一个未命名标签
                    labledata.Updata2(scenelable);//更新主标签
                    IniView(true);//更新界面
                }
                else
                {
                    GD.Print("有问题，未查询到该标签！ ");
                }
            }
        }

        /// <summary>
        ///添加标签
        /// </summary>
        public void AddLable()
        {
            TreeItem treeitem = tree.GetSelected();
            if (treeitem != null)//有选中项
            {
                List<SceneLable> list_able = labledata.GetAllSceneLabel();//获取所有标签
                string type = (string)treeitem.GetMetadata(0);
                if (type.Equals("lable"))//是标签
                {
                    string value = (string)treeitem.GetMetadata(1);
                    if (!value.Equals("root/other"))
                    {
                        SceneLable scenelable = labledata.QueryLable(list_able, value);//查询对应标签
                        if (scenelable != null)//文件数据中存在该标签
                        {
                            scenelable.AddLabel2(new SceneLable(value + "/未命名标签", true));//给标签下，加一个未命名标签
                            labledata.Updata(scenelable);//更新主标签
                            IniView(true);//更新界面
                        }
                        else
                        {
                            GD.Print("有问题，未查询到该标签！ ");
                        }
                    }
                    else
                    {
                        GD.Print("该标签下存储的是未分类标签，无法再添加标签哦！");
                    }
                }
            }
        }

        /// <summary>
        ///编辑标签名称
        /// </summary>
        public void EditLableName()
        {
            TreeItem treeitem = tree.GetSelected();
            if (treeitem != null)//有选中项
            {
                string type = (string)treeitem.GetMetadata(0);
                if (type.Equals("lable"))//是标签才能该名称
                {
                    string value = (string)treeitem.GetMetadata(1);
                    if (!value.Equals("root/other"))
                    {
                        tree.EditSelected(true);
                    }
                    else
                    {
                        GD.Print("该标签下存储的是未分类标签，无法修改标签名字哦！");
                    }
                }
            }
        }

        /// <summary>
        ///初始化页面
        /// </summary>
        public void IniView(bool IsRefresh)
        {
            if (IsRefresh)
            {
                // 清空子节点
                tree.Clear();
                tree.DeselectAll();
            }

            IniTree();

            Button button = new Button();
            button.Text = "刷新场景";
            button.Size = new Vector2(80, 31);
            button.Position = new Vector2(250, 1);
            button.ButtonDown += OnButtonRefreshView;
            MainPanelInstance.AddChild(button);
        }

        /// <summary>
        ///初始化数据，将json中的保存的场景数据与当前项目的场景数据对比，按照当前项目数据计算
        /// </summary>
        public void IniData()
        {
            //标签名称（真实名称），标签下有哪些场景
            Dictionary<string, Dictionary<string, string>> scene_dict_all = labledata.GetSceneDictAll();//json文件中记录的所有场景
            //当前项目的所有场景-临时temp_scene_dict_all
            Dictionary<string, Dictionary<string, string>> temp_scene_dict_all = scene_dict_all.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Dictionary<string, string> scene_dict = labledata.GetSceneDictAll2(scene_dict_all);//所有场景
            //当前项目的所有场景-临时temp_scene_dict
            Dictionary<string, string> temp_scene_dict = scene_dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Dictionary<string, string> sceneFiles = GetRenameFiles();       //当前项目的所有场景，所有场景必然不同名,路径也必然不同
            List<SceneLable> scene_list_all = labledata.GetAllSceneLabel(); //当前文件中保存的所有场景
            SceneLable other_lable = labledata.QueryLable(scene_list_all, "root/other");//其他分类标签
            foreach (var scene in sceneFiles)//遍历项目所有场景-检查是否已有当前场景
            {
                string name = scene.Key;
                string path = scene.Value;

                if (scene_dict.ContainsValue(path))
                {//存在相同路径的场景，就看看场景名称是否相同，相同就不管，不同该数据的场景名称
                    foreach (KeyValuePair<string, Dictionary<string, string>> scene2 in temp_scene_dict_all)
                    {
                        string lable_name = scene2.Key;//
                        Dictionary<string, string> scene_dict_ps = scene2.Value;
                        foreach (var ps in scene_dict_ps)
                        {
                            string name2 = ps.Key;
                            string path2 = ps.Value;
                            if (path.Equals(path2))//路径相同
                            {
                                if (!name2.Equals(name))//名称不同就移除原数据
                                {
                                    scene_dict_all[lable_name].Remove(name2);
                                    SceneLable temp_lable = labledata.QueryLable(scene_list_all, lable_name);
                                    temp_lable.RemoveScene(name2, path2);//移除对应场景
                                    labledata.Updata2(temp_lable);//更新标签数据
                                    other_lable.AddScene(name, path);//不存在的场景放othor下
                                }
                            }
                        }
                    }
                }
                else
                {
                    GD.Print("\n");
                    GD.Print(name + "   " + path);
                    other_lable.AddScene(name, path);//不存在的场景放othor下
                }
            }
        }


        /// <summary>
        ///初始化标签树
        /// </summary>
        public void IniTree()
        {
            TreeItem root = tree.CreateItem();
            root.SetText(0, "root");//主节点
            root.SetMetadata(0, "lable");
            root.SetMetadata(1, "root");
            List<SceneLable> lable_list = labledata.lable_list;
            for (int i = 0; i < lable_list.Count; i++)
            {
                TreeItem sceneItem = tree.CreateItem(root);
                sceneItem.SetEditable(0, true);
                sceneItem.SetText(0, lable_list[i].GetTitleName()); // 设置标签的文本为场景名称
                sceneItem.SetMetadata(0, "lable");
                sceneItem.SetMetadata(1, lable_list[i].lable_name);
                SetLable(lable_list[i], sceneItem);
            }
        }

        /// <summary>
        ///设置场景树和标签
        /// </summary>
        public void SetLable(SceneLable lable, TreeItem parentItem)
        {
            Dictionary<string, string> scene_dict = lable.GetSceneDict();
            List<SceneLable> lable_list = lable.lable_list;

            foreach (var kvp in scene_dict)
            {
                TreeItem sceneItem = tree.CreateItem(parentItem);
                sceneItem.SetText(0, kvp.Key); // 设置场景节点的文本为场景名称
                sceneItem.SetMetadata(0, "scene");
                sceneItem.SetMetadata(1, kvp.Value);//路径
            }

            for (int i = 0; i < lable_list.Count; i++)
            {
                TreeItem sub_root = tree.CreateItem(parentItem);
                sub_root.SetEditable(0, true);
                sub_root.SetText(0, lable_list[i].GetTitleName());//设置标签名称
                sub_root.SetMetadata(0, "lable");
                sub_root.SetMetadata(1, lable_list[i].lable_name);
                SetLable(lable_list[i], sub_root);
            }
        }

        /// <summary>
        ///按钮点击窗口刷新按钮
        /// </summary>
        private void OnButtonRefreshView()
        {
            IniView(true);
        }

        /// <summary>
        ///获取项目中所有场景名称和路径
        /// </summary>
        public static string[] get_path()
        {
            string scenesFolder = "res://";
            string scenesFolderPath = ProjectSettings.GlobalizePath(scenesFolder);
            string[] sceneFiles = Directory.GetFiles(scenesFolderPath, "*.tscn", SearchOption.AllDirectories);
            return sceneFiles;
        }

        /// <summary>
        ///通过场景路径获得场景名称（该名称不唯一）
        /// </summary>
        public static string get_scene_name(string sceneName)
        {
            //D:/Godot4/student/my addons/SceneView/addons\SceneView\SceneView.tscn
            string pattern = @"[^\\\/]*\.tscn";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(sceneName);
            if (match.Success)
            {
                return match.Value;
            }
            return "";
        }


        /// <summary>
        ///获取重命名后的新文件名 和 路径
        /// </summary>
        public static Dictionary<string, string> GetRenameFiles()
        {
            string[] fileEntries = Directory.GetFiles(ProjectSettings.GlobalizePath("res://"), "*.tscn", SearchOption.AllDirectories);
            Dictionary<string, string> fileDictionary = new Dictionary<string, string>();
            foreach (string fileName in fileEntries)
            {
                string baseFileName = get_scene_name(fileName);//原始文件名
                fileDictionary = Dict_Deduplication(fileDictionary, fileName);
            }
            return fileDictionary;
        }


        /// <summary>
        ///输入字典，和文件名
        /// </summary>
        public static Dictionary<string, string> Dict_Deduplication(
        Dictionary<string, string> fileDictionary, string fileName)
        {
            string baseFileName = get_scene_name(fileName);//原始文件名
            string[] fruits = fileName.Replace($"\\", "/").Split('/');
            List<string> fruitList = new List<string>(fruits);

            fruitList.RemoveAt(fruitList.Count - 1);
            Dictionary<string, string> result = Dict_Deduplication_1(fileDictionary, fileName, baseFileName, fruitList);
            return result;
        }

        /// <summary>
        ///文件路径 字典去重
        /// </summary>
        /// <param name="fileDictionary">记录字典</param>
        /// <param name="fileName">文件路径</param>
        /// <param name="baseFileName">文件当前名称</param>
        /// <param name="fruits">路径分割列表</param>
        /// <returns></returns>
        public static Dictionary<string, string> Dict_Deduplication_1(Dictionary<string, string> fileDictionary,
            string fileName, string baseFileName, List<string> fruits)
        {
            if (fileDictionary.ContainsKey(baseFileName))//还存在
            {
                string newFileName = fruits[fruits.Count - 1] + "/" + baseFileName;
                fruits.RemoveAt(fruits.Count - 1);
                return Dict_Deduplication_1(fileDictionary, fileName, newFileName, fruits);
            }
            else//没有就返回
            {
                fileDictionary[baseFileName] = fileName;
                return fileDictionary;
            }
        }

    }

}
#endif