using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace SceneCore_Space
{

    //场景标签-保存标签数据用
    public class SceneLable
    {
        //场景 标签真实名称，是一个带/的标签路径   默认标签root
        [JsonInclude]
        public string lable_name = "root";

        //父标签真实名称，是一个带/的标签路径   ""
        [JsonInclude]
        public string parent_lable_name = "";

        //对应标签下，有哪些场景-名称
        [JsonInclude]
        public List<string> dict_name = new List<string>();

        //对应标签下，有哪些场景-路径
        [JsonInclude]
        public List<string> dict_path = new List<string>();

        //对应标签下有哪些子标签,子标签下还有子标签
        [JsonInclude]
        public List<SceneLable> lable_list = new List<SceneLable>();

        public SceneLable()
        { }

        public SceneLable(string lable_name)
        {
            this.lable_name = this.lable_name + lable_name;
        }

        public SceneLable(string lable_name, bool _is)
        {
            this.lable_name = lable_name;
        }

        public SceneLable ParentLable(List<SceneLable> list)
        {
            return QueryLable(list, parent_lable_name);
        }

        /// <summary>
        ///通过 标签名称，获取标签本身
        /// </summary>
        public SceneLable QueryLable(List<SceneLable> list, string name)
        {
            if (name.Equals(lable_name))//就是自身
            {
                return this;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (name.Equals(list[i].lable_name))
                {
                    return list[i];
                }
            }
            return null;//没查到
        }

        /// <summary>
        ///通过 标签名称，获取标签本身
        /// </summary>
        public SceneLable QueryLable2(List<SceneLable> list, TreeItem item)
        {
            if (item != null)
            {
                string name = (string)item.GetMetadata(1);
                if (name.Equals(lable_name))//就是自身
                {
                    return this;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (name.Equals(list[i].lable_name))
                    {
                        return list[i];
                    }
                }
            }
            return null;//没查到
        }

        /// <summary>
        ///通过 标签名称，更新对应标签
        /// </summary>
        public bool Updata(SceneLable lable)
        {
            for (int i = 0; i < lable_list.Count; i++)
            {
                if (lable.lable_name.Equals(lable_list[i].lable_name))
                {
                    lable_list[i] = lable;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///通过 标签名称，更新对应标签(可以更新标签的子标签)
        /// </summary>
        /// <summary>
        /// 通过标签名称，更新对应标签及其子标签
        /// </summary>
        /// <param name="lable">要更新的标签</param>
        /// <returns>更新是否成功</returns>
        public bool Updata2(SceneLable lable)
        {
            if (lable_name.Equals(lable.lable_name))
            {
                // 如果当前标签就是要更新的标签，则直接替换为传入的标签
                lable_name = lable.lable_name;
                parent_lable_name = lable.parent_lable_name;
                dict_name = new List<string>(lable.dict_name);
                dict_path = new List<string>(lable.dict_path);
                // 清空原有的子标签列表并添加传入标签的子标签列表
                lable_list.Clear();
                lable_list.AddRange(lable.lable_list);
                return true;
            }

            // 在子标签列表中查找并更新对应子标签
            for (int i = 0; i < lable_list.Count; i++)
            {
                if (lable.lable_name.Equals(lable_list[i].lable_name))//如果是子标签就直接成功
                {
                    lable.lable_name = parent_lable_name + "/" + lable.GetTitleName(); ;//标签的父标签设置
                    lable.parent_lable_name = parent_lable_name;//标签的父标签设置
                    lable_list[i] = lable; // 替换子标签为传入的标签
                    return true;
                }
                // 递归调用 Updata2 方法更新子标签的子标签
                if (lable_list[i].Updata2(lable))
                {
                    return true;
                }
            }
            return false; // 没有找到对应标签，更新失败
        }



        /// <summary>
        ///获取该场景下有哪些场景，标签名,场景列表
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> GetSceneDictAll()
        {
            Dictionary<string, Dictionary<string, string>> dict = new Dictionary<string, Dictionary<string, string>>();
            dict[lable_name] = GetSceneDict();//自身有的场景
            for (int i = 0; i < lable_list.Count; i++)
            {
                Dictionary<string, string> dict2 = new Dictionary<string, string>(lable_list[i].GetSceneDict()); // 复制 dict2 的内容到新的字典
                if (!dict.ContainsKey(lable_list[i].lable_name))
                {
                    dict.Add(lable_list[i].lable_name, dict2);
                }
                else
                {
                    GD.Print("重复键：" + lable_list[i].lable_name);
                }
            }
            return dict;
        }

        /// <summary>
        ///获取该场景下有哪些场景,纯场景
        /// </summary>
        public Dictionary<string, string> GetSceneDictAll2(Dictionary<string, Dictionary<string, string>> allscenedict)
        {
            Dictionary<string, string> scene_dict = new Dictionary<string, string>();
            foreach (var item in allscenedict)
            {
                foreach (var kvp in item.Value)
                {
                    scene_dict[kvp.Key] = kvp.Value;
                }
            }
            return scene_dict;
        }

        /// <summary>
        ///获取该场景下有哪些场景
        /// </summary>
        public Dictionary<string, string> GetSceneDict()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < dict_name.Count; i++)
            {
                string[] array = dict_name[i].Split('/');
                //dict[array[array.Length - 1]] = dict_path[i];
                dict[dict_name[i]] = dict_path[i];
            }
            return dict;
        }


        /// <summary>
        ///获取标签显示用名称
        /// </summary>
        public string GetTitleName()
        {
            string[] array = lable_name.Split('/');
            return array[array.Length - 1];
        }


        /// <summary>
        ///获取父标签显示用名称
        /// </summary>
        public string GetParentTitleName()
        {
            return parent_lable_name;
        }

        /// <summary>
        ///在直属标签中 移除一个场景
        /// </summary>
        public bool RemoveScene(string scene_name, string scene_path)
        {
            int index1 = dict_name.IndexOf(scene_name);
            if (index1 != -1)//检查场景下是否已经有相同场景名称
            {
                if (dict_path.IndexOf(scene_path) != -1)//检查场景下是否已经有相同场景路径
                {
                    dict_name.RemoveAt(index1);//移除该场景名称
                    dict_path.RemoveAt(index1);//移除该场景路径
                    return true;
                }
                else//理论上不太可能
                {
                    GD.Print("理论上不太可能运行到这里！");
                    dict_name.RemoveAt(index1);//移除该场景名称
                    dict_path.RemoveAt(index1);//移除该场景路径
                    return true;
                }
            }
            else//没查到
            {
                return false;
            }
        }


        /// <summary>
        /// 在该标签及其所有子标签中移除指定场景
        /// </summary>
        public bool RemoveSceneFromAllLabels(string sceneName, string scenePath)
        {
            bool removed = false;
            // 首先尝试从当前标签中移除场景
            if (RemoveScene(sceneName, scenePath))
            {
                removed = true;
            }

            // 然后递归处理所有子标签
            foreach (SceneLable label in lable_list)
            {
                if (label.RemoveScene(sceneName, scenePath)) // 递归调用子标签的 RemoveScene 方法
                {
                    removed = true;
                }
            }
            return removed;
        }


        /// <summary>
        ///添加一个场景
        /// </summary>
        public bool AddScene(string scene_name, string scene_path)
        {
            if (dict_name.Contains(scene_name))//检查场景下是否已经有相同场景名称
            {
                return false;
            }
            else
            {
                dict_name.Add(scene_name);
                dict_path.Add(scene_path);
                return true;
            }
        }


        public bool MoveBeforeScene(string sceneNameToMove)
        {
            int index = dict_name.IndexOf(sceneNameToMove);
            if (index > 0)
            {
                string sceneName = dict_name[index];
                string scenePath = dict_path[index];
                dict_name.RemoveAt(index);
                dict_path.RemoveAt(index);
                dict_name.Insert(index - 1, sceneName);
                dict_path.Insert(index - 1, scenePath);
                return true;
            }
            return false;
        }

        public bool MoveAfterScene(string sceneNameToMove)
        {
            int index = dict_name.IndexOf(sceneNameToMove);
            if (index >= 0 && index < dict_name.Count - 1)
            {
                string sceneName = dict_name[index];
                string scenePath = dict_path[index];
                dict_name.RemoveAt(index);
                dict_path.RemoveAt(index);
                dict_name.Insert(index + 1, sceneName);
                dict_path.Insert(index + 1, scenePath);
                return true;
            }
            return false;
        }

        public bool MoveBeforeLable(string lableNameToMove)
        {
            int index = lable_list.FindIndex(l => l.lable_name == lableNameToMove);
            if (index > 0)
            {
                SceneLable lable = lable_list[index];
                lable_list.RemoveAt(index);
                lable_list.Insert(index - 1, lable);
                return true;
            }
            return false;
        }

        public bool MoveAfterLable(string lableNameToMove)
        {
            int index = lable_list.FindIndex(l => l.lable_name == lableNameToMove);
            if (index >= 0 && index < lable_list.Count - 1)
            {
                SceneLable lable = lable_list[index];
                lable_list.RemoveAt(index);
                lable_list.Insert(index + 1, lable);
                return true;
            }
            return false;
        }



        /// <summary>
        ///添加一个标签
        /// </summary>
        public bool AddLabel(SceneLable lable)
        {
            List<string> name_list = getlabel_name2();
            foreach (string name in name_list)//检查是否子标签中已经有这种标签了
            {
                if (name.Equals(lable.lable_name))
                {
                    return false;
                }
            }
            string new_name = lable_name + "/" + lable.GetTitleName();
            lable.lable_name = new_name;
            lable.parent_lable_name = lable_name;
            lable_list.Add(lable);
            return true;
        }


        //添加未命名标签
        public void AddLabel2(SceneLable lable)
        {
            string lablename = lable.lable_name;
            int i = 0;
            while (!AddLabel(lable))//检查添加标签，加不上就说明已经有该标签了
            {
                lable.lable_name = lablename + i;
                i++;
            }
        }

        /// <summary>
        ///移除对应标签
        /// </summary>
        public void Remove(SceneLable lable)
        {
            lable_list.Remove(lable);
        }


        /// <summary>
        ///移除对应序号的标签
        /// </summary>
        public void remove_index(int i)
        {
            lable_list.RemoveAt(i);
        }


        /// <summary>
        ///获取该标签下所有标签名称
        /// </summary>
        public List<string> getlabel_name()
        {
            List<string> str_list = new List<string>();
            foreach (SceneLable lable in lable_list)
            {
                str_list.AddRange(lable.getlabel_name());

            }
            return str_list;
        }


        /// <summary>
        ///获取该标签下，子标签的所有名称（不包含子标签的子标签）
        /// </summary>
        public List<string> getlabel_name2()
        {
            List<string> str_list = new List<string>();
            foreach (SceneLable lable in lable_list)
            {
                str_list.Add(lable.lable_name);
            }
            return str_list;
        }

        /// <summary>
        ///获取该标签下，所有子标签，包括子标签的子标签
        /// </summary>
        public List<SceneLable> GetAllSceneLabel()
        {
            List<SceneLable> str_list = new List<SceneLable>(); // 创建一个新的列表对象来存储结果

            foreach (SceneLable lable in lable_list)
            {
                str_list.Add(lable); // 将当前标签添加到结果列表中
                str_list.AddRange(lable.GetAllSceneLabel()); // 递归添加子标签
            }

            return str_list;
        }


        public override string ToString()
        {
            string lable_list_str = "";
            string dict_list = "[";
            foreach (var lable in lable_list)
            {
                lable_list_str += ",\n\t" + lable.ToString();
            }
            for (int i = 0; i < dict_name.Count; i++)
            {
                dict_list += "\n{" + dict_name[i] + "," + dict_path[i] + "},";
            }

            string all = "lable_name:" + lable_name + ",parent_lable_name:" + parent_lable_name +
                 ",[" + lable_list_str + "],\n\tscene_list:" + dict_list + "]";

            return all;
        }
    }

}