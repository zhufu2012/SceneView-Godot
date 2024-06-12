﻿using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace SceneCore_Space
{

    public partial class SceneTree : Tree
    {
        private PopupMenu popupMenu;//菜单
        public SceneLable labledata;
        private List<Texture2D> ico_list = new List<Texture2D>();
        private Control MainPanelInstance;//主节目
        public SceneTree()
        { }

        public SceneTree(PopupMenu popupMenu, SceneLable labledata, List<Texture2D> ico_list)
        {
            Columns = 3;
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
            this.labledata = labledata;
            this.ico_list = ico_list;

            popupMenu.Hide();//隐藏
            popupMenu.Connect(PopupMenu.SignalName.IdPressed, new Callable(this, MethodName.OnMenu));
            IniData();//初始化数据
            IniView(false);//初始化树
        }

        public override void _Ready()
        {
            base._Ready();
        }

        public override void _PhysicsProcess(double delta)
        {
            //GD.Print(1);
            base._PhysicsProcess(delta);
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
            TreeItem target_itm = GetItemAtPosition(atPosition);//目标
            TreeItem treeitem = (TreeItem)data.AsGodotObject();//拖动对象
            DropModeFlags = 1 | 2;
            //是否为同级
            bool IsParent = treeitem.GetParent().Equals(target_itm.GetParent());
            if (treeitem != null)//拖动对象
            {
                string type = (string)treeitem.GetMetadata(0);
                string value = (string)treeitem.GetMetadata(1);
                string target_type = (string)treeitem.GetMetadata(0);
                string target_value = (string)treeitem.GetMetadata(1);
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
                    {//.标签可以拖动，不能拖到场景中
                        return false;
                    }
                    if (type.Equals("scene") && target_type.Equals("lable") && target_pos == -1)
                    {//场景不能放在标签前面
                        return false;
                    }
                    if (type.Equals("scene") && target_type.Equals("scene") && target_pos == 0)
                    {//场景不能放在场景前面
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

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            //获取目标拖放位置，-1,0,1分别代表在某项之前、之上和之后
            int target_pos = GetDropSectionAtPosition(atPosition);

            TreeItem treeitem = (TreeItem)data.AsGodotObject();//拖动对象
            TreeItem target_itm = GetItemAtPosition(atPosition);//目标

            List<SceneLable> list = labledata.GetAllSceneLabel();

            string type = (string)treeitem.GetMetadata(0);//拖动对象
            string value = (string)treeitem.GetMetadata(1);//拖动对象

            string target_type = (string)target_itm.GetMetadata(0);//目标
            string target_value = (string)target_itm.GetMetadata(1);//目标

            SceneLable target_itm_lable = labledata.QueryLable2(list, target_itm);//对象
            SceneLable treeitem_lable = null;//拖动
            if (target_type != "scene")////这里拖动对象是场景的情况下，不是同级节点---祝福注释-这里有问题
            {
                treeitem_lable = labledata.QueryLable2(list, treeitem);
            }
            else
            {
                treeitem_lable = labledata;
            }
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
                            treeitem_lable.MoveBeforeScene((string)target_itm.GetMetadata(1));
                        }
                        else//不是同级节点
                        {
                            treeitem.GetParent().RemoveChild(treeitem);//从父节点去除

                            SceneLable ParentLable = treeitem_lable.ParentLable(list);
                            //ParentLable.RemoveScene();
                            target_itm.GetParent().AddChild(treeitem);//在目标的父节点上加个子节点
                            treeitem.MoveBefore(target_itm);//移动该节点到目标前面
                            treeitem_lable.MoveBeforeScene((string)target_itm.GetMetadata(1));
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

            /*if target_itm in get_items(data[0]):
		return # 禁止移动
	match target_pos:
            -1: # 拖放到了某个 TreeItem 之前
			# 根据是否同级进行区别处理
			if data[0].get_parent() == target_itm.get_parent(): # 如果同级
				data[0].move_before(target_itm)

            else:
				data[0].get_parent().remove_child(data[0])         # 先从原来的父节点删除
				target_itm.add_child(data[0])                      # 添加到目标位置的TreeItem
				data[0].move_before(target_itm)


        0:  # 拖放到了某个TreeItem上
			data[0].get_parent().remove_child(data[0])         # 先从原来的父节点删除
			target_itm.add_child(data[0])                      # 添加到目标位置的TreeItem
		1: # 拖放到了某个TreeItem之后
			# 根据是否同级进行区别处理
			if data[0].get_parent() == target_itm.get_parent(): # 如果同级
				data[0].move_after(target_itm)

            else:
				data[0].get_parent().remove_child(data[0])         # 先从原来的父节点删除
				target_itm.add_child(data[0])                      # 添加到目标位置的TreeItem
				data[0].move_after(target_itm)

# 返回某个TreeItem下所有子孙节点的集合
func get_items(item:TreeItem) -> Array[TreeItem]:
	var arr:Array[TreeItem]

    if item.get_child_count() > 0:
		arr.append_array(item.get_children())

        for chd in item.get_children():

            arr.append_array(get_items(chd))

    return arr*/

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
            //else if (id == 1)
            //{//编辑标签名称
            //    EditLableName();
            //}
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
                            scenelable.AddLabel2(new SceneLable(value + "/未命名标签", true));//给标签下，加一个未命名标签
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
        /*public void EditLableName()
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
        }*/

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

		
		
		public void SetTreeItemEdited()
		{
			TreeItem treeitem = GetEdited();
			if (treeitem != null)
            {
				string type = (string)treeitem.GetMetadata(0);
                if (type.Equals("lable"))//是标签才能
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
                    other_lable.AddScene(name, path);//不存在的场景放othor下
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
            for (int i = 0; i < lable_list.Count; i++)
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
            for (int i = 0; i < lable_list.Count; i++)
            {
                TreeItem sub_root = CreateItem(parentItem);
                sub_root.SetEditable(0, true);
                sub_root.SetText(0, lable_list[i].GetTitleName());//设置标签名称
                sub_root.SetIcon(0, ico_list[1]);
                sub_root.SetIconMaxWidth(0, 16);
                sub_root.SetMetadata(0, "lable");
                sub_root.SetMetadata(1, lable_list[i].lable_name);
                sub_root.SetMetadata(2, lable_list[i].lable_name);
                sub_list.Add(sub_root);
            }
            foreach (var kvp in scene_dict)
            {
                TreeItem sceneItem = CreateItem(parentItem);
                sceneItem.SetText(0, kvp.Key); // 设置场景节点的文本为场景名称
                sceneItem.SetIcon(0, ico_list[2]);
                sceneItem.SetIconMaxWidth(0, 16);
                sceneItem.SetMetadata(0, "scene");
                sceneItem.SetMetadata(1, kvp.Value);//路径
            }
            for (int i = 0; i < sub_list.Count; i++)
            {
                SetLable(lable_list[i], sub_list[i]);
            }
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