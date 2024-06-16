using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SceneCore_Space
{

    public partial class SceneTree : Tree
    {
        private PopupMenu popupMenu;//菜单
        public SceneLable labledata;
        private Control MainPanelInstance;//主节目
                                          //维护json文件
        SaveLoadData saveLoadData = null;
        List<Texture2D> ico_list = new List<Texture2D>();

        public int MAXTIME = 10;//最大保存间隔 10帧
        //保存间隔
        int SaveTime = 0;

        ///初始化加载图片
        public void InitIco()
        {
            ico_list.Add(GD.Load<Texture2D>("res://addons/SceneView/img/主场景.png"));
            ico_list.Add(GD.Load<Texture2D>("res://addons/SceneView/img/标签.png"));
            ico_list.Add(GD.Load<Texture2D>("res://addons/SceneView/img/场景.png"));
        }

        public SceneTree()
        { }



        public SceneTree(PopupMenu popupMenu)
        {
            InitIco();
            saveLoadData = new SaveLoadData();
            labledata = saveLoadData.GetSceneLabelList();
            Columns = 2;
            AllowSearch = true;
            AllowRmbSelect = true;
            EnableRecursiveFolding = true;
            DropModeFlags = (int)(DropModeFlagsEnum.OnItem | DropModeFlagsEnum.Inbetween);
            //DROP_MODE_ON_ITEM | DROP_MODE_INBETWEEN
            SelectMode = SelectModeEnum.Row;
            ScrollVerticalEnabled = true;
            Position = new Vector2(0, 0);
            Size = new Vector2(331, 645);
            Name = "Tree";

            Connect(Tree.SignalName.ItemMouseSelected, new Callable(this, MethodName.MouseItemSelected));
            ItemActivated += SetTreeItemEdited;//双击某一项,设置可编辑
            ItemEdited += OnTreeItemEdited;//编辑后修改，标签名称
            this.popupMenu = popupMenu;
            popupMenu.Hide();//隐藏
            popupMenu.Connect(PopupMenu.SignalName.IdPressed, new Callable(this, MethodName.OnMenu));
            IniData();//初始化数据
            IniView(false);//初始化树
        }

        public override void _PhysicsProcess(double delta)
        {
            SaveTime += 1;
            if (SaveTime > MAXTIME)
            {
                SaveTime = 0;
                SaveData();
            }
            base._PhysicsProcess(delta);
        }

        public override void _ExitTree()
        {
            SaveData();
        }

        //获取拖动预览
        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (GetTree() != null)
            {
                TreeItem item = GetItemAtPosition(atPosition);
                if (item != null)
                {
                    SetDragPreview(MakeDragPreview(item));
                    return item;
                }
            }
            return base._GetDragData(atPosition);
        }

        /// <summary>
        ///构造预览界面
        /// </summary>
        public Control MakeDragPreview(TreeItem item)
        {
            Control con = new Control();
            string type = (string)item.GetMetadata(0);//拖动对象的类型
            string value = (string)item.GetMetadata(1);//拖动对象的值
            if (type.Equals("lable"))//如果是标签
            {
                List<TreeItem> list = GetItems(item);//拖动对象子节点列表
                if (list.Count > 0)//有子节点 祝福注释-暂时
                {
                    Button button = new Button();
                    button.Text = item.GetText(0);
                    con.AddChild(button);
                }
                else
                {
                    Button button = new Button();
                    button.Text = item.GetText(0);
                    con.AddChild(button);
                }

            }
            return con;
        }

        /// <summary>
        ///获取子节点
        /// </summary>
        public List<TreeItem> GetItems(TreeItem item)
        {
            List<TreeItem> list = new List<TreeItem>();
            if (item.GetChildCount() > 0)
            {
                list.AddRange(item.GetChildren());
                foreach (TreeItem chd in item.GetChildren())
                {
                    list.AddRange(GetItems(chd));
                }
            }
            return list;
        }

        /// <summary>
        ///检查是否可拖动
        /// </summary>
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            //获取目标拖放位置，-1,0,1分别代表在某项之前、之上和之后
            int target_pos = GetDropSectionAtPosition(atPosition);
            TreeItem treeitem = (TreeItem)data.AsGodotObject();//拖动对象
            TreeItem target_itm = GetItemAtPosition(atPosition);//拖动到的目标
            if (target_itm != null)
            {
                DropModeFlags = 1 | 2;
                //是否为同级
                //bool IsParent = treeitem.GetParent().Equals(target_itm.GetParent());
                if (treeitem != null)//拖动对象
                {
                    string type = (string)treeitem.GetMetadata(0);
                    string value = (string)treeitem.GetMetadata(1);
                    string target_type = (string)target_itm.GetMetadata(0);//目标类型
                    string target_value = (string)target_itm.GetMetadata(1);//目标的值
                    if (!value.Equals("root") && !value.Equals("root/other"))
                    {//拖动对象不能是root和root/other
                        if (target_value.Equals("root") && (target_pos == -1 || target_pos == 1))
                        {//目标是root，不能拖动到root上和root下
                            return false;
                        }
                        if (target_value.Equals("root/other") && target_pos == -1)
                        {//标签和场景都不能拖到other前
                            return false;
                        }
                        if (target_type.Equals("lable") && target_value.Equals("root/other") && target_pos == 0)
                        {//标签不能到other中
                            return false;
                        }
                        if (type.Equals("lable") && target_type.Equals("scene") && target_pos == 0)
                        {//.标签可以拖动，不能拖到场景中间
                            return false;
                        }
                        if (type.Equals("scene") && target_type.Equals("lable") && target_pos == -1)
                        {//场景不能放在标签前面
                            return false;
                        }
                        if (type.Equals("scene") && target_type.Equals("scene") && target_pos == 0)
                        {//场景不能放在场景中间
                            return false;
                        }
                        List<TreeItem> list = GetItems(treeitem);//拖动对象子节点
                        if (!list.Contains(target_itm))//目标如果不是其子节点
                        {//目标不是其子节点，能拖动
                            if (target_itm != treeitem)//拖动对象不是目标
                            {
                                return true;
                            }
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

            }
            return false;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            //获取目标拖放位置，-1,0,1分别代表在某项之前、之上和之后
            int target_pos = GetDropSectionAtPosition(atPosition);

            TreeItem treeitem = (TreeItem)data.AsGodotObject();//拖动对象
            TreeItem target_itm = GetItemAtPosition(atPosition);//目标

            string type = (string)treeitem.GetMetadata(0);//拖动对象

            string target_type = (string)target_itm.GetMetadata(0);//目标
            //是否为同级
            bool IsParent = treeitem.GetParent().Equals(target_itm.GetParent());

            if (type.Equals("lable"))//如果拖动对象是标签
            {

            }
            else//对象是场景
            {
                switch (target_pos)
                {
                    case -1://目标前面
                        if (IsParent)//是同级节点
                        {//将这个 TreeItem 移动至给定的 item 之前
                            treeitem.MoveBefore(target_itm);

                        }
                        else//不是同级节点
                        {
                            treeitem.GetParent().RemoveChild(treeitem);//从父节点去除
                            //ParentLable.RemoveScene();
                            target_itm.GetParent().AddChild(treeitem);//在目标的父节点上加个子节点
                            treeitem.MoveBefore(target_itm);//移动该节点到目标前面
                        }
                        break;
                    case 0://目标下
                        treeitem.GetParent().RemoveChild(treeitem);//从父节点去除
                        target_itm.AddChild(treeitem);//在目标的父节点上加个子节点
                        break;
                    case 1:
                        if (IsParent)//是同级节点
                        {//将这个 TreeItem 移动至给定的 item 之前
                            treeitem.MoveAfter(target_itm);
                        }
                        else//不是同级节点
                        {
                            treeitem.GetParent().RemoveChild(treeitem);//从父节点去除
                            target_itm.GetParent().AddChild(treeitem);//在目标的父节点上加个子节点
                            treeitem.MoveAfter(target_itm);//移动该节点到目标前面
                        }

                        break;
                    default:
                        break;
                }
            }
        }


        /// <summary>
        /// 打开某个场景
        /// </summary>
        public bool OpenScene(string path)
        {
            EditorInterface face = EditorInterface.Singleton;
            string[] strarr = face.GetOpenScenes();

            for (int i = 0; i < strarr.Length; i++)
            {
                GD.Print(ProjectSettings.GlobalizePath(strarr[i]));
                GD.Print(path.Replace("\\", "/"));
                if (ProjectSettings.GlobalizePath(strarr[i]).Equals(path.Replace("\\", "/")))
                    return false;
            }
            face.OpenSceneFromPath(path);
            return true;
        }

        public void SaveData()
        {
            SceneLable Lable = new SceneLable();
            TreeItem root = GetRoot();
            Godot.Collections.Array<TreeItem> array = root.GetChildren();
            for (int i = 0; i < array.Count; i++)
            {
                TreeItem treeItem = array[i];//root下的子节点
                string type = (string)treeItem.GetMetadata(0);
                string path = (string)treeItem.GetMetadata(1);
                if (type.Equals("lable") && path.Equals("root/other"))//other标签
                {
                    SceneLable LableOther = new SceneLable("root/other", true);//other标签
                    LableOther.parent_lable_name = "root";
                    for (int j = 0; j < treeItem.GetChildCount(); j++)
                    {
                        TreeItem treeItem2 = treeItem.GetChild(j);
                        LableOther.AddScene(treeItem2.GetText(0), (string)treeItem2.GetMetadata(1));
                    }
                    Lable.AddLabel(LableOther);
                }
                else if (type.Equals("lable"))//root下的非other标签
                {
                    SceneLable sceneLable = GetLable(treeItem, treeItem.GetText(0), "root");
                    Lable.AddLabel(sceneLable);
                }
                else
                {
                    Lable.AddScene(treeItem.GetText(0), path);
                }
            }
            labledata = Lable;
            SaveLoadData.parent_lable = Lable;
            if (saveLoadData != null)
                saveLoadData.SaveData();//保存数据
        }

        //获取子节点的SceneLable数据
        public SceneLable GetLable(TreeItem item, string name, string pr_name)
        {
            SceneLable lable = new SceneLable(pr_name + "/" + name, true);
            for (int j = 0; j < item.GetChildCount(); j++)
            {
                TreeItem treeItem = item.GetChild(j);
                string type = (string)treeItem.GetMetadata(0);
                string path = (string)treeItem.GetMetadata(1);
                if (type.Equals("lable"))//标签
                {
                    //GD.Print("\n");
                    //GD.Print(pr_name);
                    //GD.Print(name);
                    //GD.Print(item.GetText(0));
                    //GD.Print("\n");
                    SceneLable sceneLable = GetLable(treeItem, treeItem.GetText(0), pr_name + "/"  + item.GetText(0));
                    
                    lable.AddLabel(sceneLable);
                }
                else//场景
                {
                    lable.AddScene(treeItem.GetText(0), path);
                }
            }
            return lable;
        }




        /// <summary>
        ///鼠标选中某选项
        /// </summary>
        public void MouseItemSelected(Vector2 position, int mouse_button_index)
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
                TreeItem treeitem = GetSelected();//当前选中
                if (treeitem != null)
                {
                    string type = (string)treeitem.GetMetadata(0);
                    if (type.Equals("lable"))//标签
                    {
                        popupMenu.Position = new Vector2I((int)position.X + 14, (int)(position.Y) + 100);
                        popupMenu.Show();
                    }
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
            {//编辑标签名称
                EditLableName();
            }
            else
            {
                DeleteLable();
            }
        }

        /// <summary>
        ///添加标签
        /// </summary>
        public void AddLable()
        {
            TreeItem treeitem = GetSelected();
            if (treeitem != null)//有选中项
            {
                string type = (string)treeitem.GetMetadata(0);
                if (type.Equals("lable"))//是标签
                {
                    string value = (string)treeitem.GetMetadata(1);
                    if (!value.Equals("root/other"))
                    {
                        List<SceneLable> list_able = labledata.GetAllSceneLabel();//获取所有标签
                        SceneLable scenelable = labledata.QueryLable(list_able, value);//查询对应标签
                        if (scenelable != null)//文件数据中存在该标签
                        {
                            SceneLable new_lable = new SceneLable(value + "/未命名标签", true);
                            scenelable.AddLabel2(new_lable);//给标签下，加一个未命名标签
                            labledata.Updata(scenelable);//更新主标签 祝福注释
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
            TreeItem treeitem = GetSelected();
            if (treeitem != null)//有选中项
            {
                string type = (string)treeitem.GetMetadata(0);
                if (type.Equals("lable"))//是标签才能修改名称
                {
                    string value = (string)treeitem.GetMetadata(1);
                    if (!value.Equals("root/other"))
                    {
                        if (!value.Equals("root"))
                        {
                            EditSelected(true);
                        }
                        else
                        {
                            GD.Print("该标签是根标签，无法修改！");
                        }
                    }
                    else
                    {
                        GD.Print("该标签下存储的是未分类场景，无法修改该标签名字哦！");
                    }
                }
            }
        }

        /// <summary>
        ///删除标签
        /// </summary>
        public void DeleteLable()
        {
            TreeItem treeitem = GetSelected();
            if (treeitem != null)//有选中项
            {
                string type = (string)treeitem.GetMetadata(0);
                if (type.Equals("lable"))//是标签才能删除，删除后所有子标签消失，所有子场景修改为父节点的场景
                {
                    string value = (string)treeitem.GetMetadata(1);
                    if (!value.Equals("root/other"))//其他节点
                    {
                        if (!value.Equals("root"))
                        {
                            List<SceneLable> list_able = labledata.GetAllSceneLabel();//获取所有标签
                            SceneLable scenelable = labledata.QueryLable(list_able, value);//查询对应标签
                            if (scenelable != null)//文件数据中存在该标签
                            {
                                Dictionary<string, string> scene_dict_all = scenelable.GetSceneDictAll2(scenelable.GetSceneDictAll());
                                SceneLable parent_scenelable = labledata.QueryLable(list_able, scenelable.parent_lable_name);//查询父标签
                                if (scene_dict_all.Count > 0)//有子场景
                                {
                                    foreach (var key in scene_dict_all)
                                        parent_scenelable.AddScene(key.Key, key.Value);
                                }
                                parent_scenelable.Remove(scenelable);//去掉该节点
                                labledata.Updata2(parent_scenelable);//更新数据
                                IniView(true);//更新界面
                            }
                        }
                    }
                    else
                        GD.Print("该标签是根标签，无法删除！");
                }
                else
                    GD.Print("该标签下存储的是未分类场景，无法删除该标签名字哦！");
            }
        }


        /// <summary>
        /// 双击某一项,设置可编辑///没有用到
        /// </summary>
        public void SetTreeItemEdited()
        {
            TreeItem treeitem = GetSelected();//当前选中
            GD.Print(treeitem);
            if (treeitem != null)
            {
                string type = (string)treeitem.GetMetadata(0);
                GD.Print(type);
                if (type.Equals("scene"))//是标签才能
                {
                    string value = (string)treeitem.GetMetadata(1);
                    OpenScene(value);
                }
            }
        }

        /// <summary>
        ///修改标签名称
        /// </summary>
        public void OnTreeItemEdited()
        {
            TreeItem treeitem = GetEdited();
            if (treeitem != null)
            {
                List<SceneLable> list_able = labledata.GetAllSceneLabel();//获取所有标签
                string value = (string)treeitem.GetMetadata(1);
                SceneLable scenelable = labledata.QueryLable(list_able, value);//查询对应标签
                if (scenelable != null)//文件数据中存在该标签
                {

                    scenelable.lable_name = scenelable.parent_lable_name + "/" + treeitem.GetText(0);//新名称
                    GD.Print("修改的：" + treeitem.GetText(0));
                    GD.Print("修改的：" + scenelable);
                    //labledata.Updata2(scenelable);//更新主标签
                    GD.Print("修改的：" + labledata.Updata2(scenelable));
                    IniView(true);//更新界面
                }
                else
                {
                    GD.Print("有问题，未查询到该标签！ ");
                }
            }
        }

        /// <summary>
        ///初始化树
        /// </summary>
        public void IniView(bool IsRefresh)
        {
            if (IsRefresh)
            {
                // 清空子节点
                Clear();
                DeselectAll();
            }
            IniTree();
        }


        /// <summary>
        ///初始化数据，将json中的保存的场景数据与当前项目的场景数据对比，按照当前项目数据计算
        /// </summary>
        public void IniData()
        {
            //标签名称（真实名称），标签下有哪些场景
            Dictionary<string, Dictionary<string, string>> scene_dict_all = labledata.GetSceneDictAll();//json文件中记录的所有场景
            Dictionary<string, string> scene_dict = labledata.GetSceneDictAll2(scene_dict_all);//json文件中记录,统一为一个场景字典,<名称，路径>
            Dictionary<string, string> sceneFiles = GetRenameFiles();     //当前项目的所有场景，所有场景必然不同名,路径也必然不同
            List<SceneLable> scene_list_all = labledata.GetAllSceneLabel(); //当前文件中保存的所有场景
            SceneLable other_lable = labledata.QueryLable(scene_list_all, "root/other");//其他分类标签

            GD.Print("文件里面的\n");
            foreach (var k in sceneFiles)
            {
                GD.Print(k.Key + "   " + k.Value);
            }
            GD.Print("json里面的\n");
            foreach (var k in scene_dict)
            {
                GD.Print(k.Key + "   " + k.Value);
            }

            foreach (var scene in scene_dict)//遍历项目所有场景-检查是否已有当前场景
            {
                if (sceneFiles.ContainsValue(scene.Value))//检查json中所有场景中，是否已经有了该场景数据，
                {//存在相同路径（路径相同名称也一定相同）的场景，就不管

                }
                else
                {
                    other_lable.AddScene(scene.Key, scene.Value);//不存在的场景放othor下
                }
            }
        }


        /// <summary>
        ///初始化标签树
        /// </summary>
        public void IniTree()
        {
            TreeItem root = CreateItem();
            root.SetText(0, "root");//主节点
            root.SetIcon(0, ico_list[0]);
            root.SetIconMaxWidth(0, 16);
            root.SetMetadata(0, "lable");
            root.SetMetadata(1, "root");
            List<SceneLable> lable_list = labledata.lable_list;
            for (int i = 0; i < lable_list.Count; i++)//主节点下标签
            {
                TreeItem sceneItem = CreateItem(root);
                sceneItem.SetEditable(0, true);
                sceneItem.SetText(0, lable_list[i].GetTitleName()); // 设置标签的文本为标签名称
                sceneItem.SetIcon(0, ico_list[1]);
                sceneItem.SetIconMaxWidth(0, 16);
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
            List<TreeItem> sub_list = new List<TreeItem>();
            for (int i = 0; i < lable_list.Count; i++)//添加标签节点
            {
                TreeItem sub_root = CreateItem(parentItem);
                sub_root.SetEditable(0, true);
                sub_root.SetText(0, lable_list[i].GetTitleName());//设置标签名称 显示名称
                sub_root.SetIcon(0, ico_list[1]);
                sub_root.SetIconMaxWidth(0, 16);
                sub_root.SetMetadata(0, "lable");
                sub_root.SetMetadata(1, lable_list[i].lable_name);//标签名称（路径）
                sub_list.Add(sub_root);
            }
            foreach (var kvp in scene_dict)//添加场景节点
            {
                TreeItem sceneItem = CreateItem(parentItem);
                sceneItem.SetText(0, kvp.Key); // 设置场景节点的文本为场景名称
                sceneItem.SetIcon(0, ico_list[2]);
                sceneItem.SetIconMaxWidth(0, 16);
                sceneItem.SetMetadata(0, "scene");
                sceneItem.SetMetadata(1, kvp.Value);//路径
            }
            for (int i = 0; i < sub_list.Count; i++)//添加标签节点下数据
            {
                SetLable(lable_list[i], sub_list[i]);
            }
        }



        /// <summary>
        ///获取项目中所有场景 场景名 和 路径的字典
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
    }
}
