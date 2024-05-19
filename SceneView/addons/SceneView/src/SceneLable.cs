


using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace SceneCore_Space
{
    //场景标签-保存标签数据用
    public class SceneLable
    {
        //场景 标签真实名称，是一个带/的标签路径   默认标签root/SceneView/other
        [JsonInclude]
        public string lable_name = "root";

        //父标签真实名称，是一个带/的标签路径   ""
        [JsonInclude]
        public string parent_lable_name = "";

        //对应标签下，有哪些场景-名称
        //public Dictionary<string, string>[] list = new Dictionary<string, string>[0];
        [JsonInclude]
        public List<string> dict_name = new List<string>();
        //对应标签下，有哪些场景-路径
        [JsonInclude]
        public List<string> dict_path = new List<string>();

        //对应标签下有哪些子标签,子标签下还有子标签
        //public SceneLable[] lable_list = new SceneLable[0];
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

        /// <summary>
        ///通过 标签名称，获取标签本身
        /// </summary>
        public SceneLable QueryLable(List<SceneLable> list, string name)
        {
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
                dict[array[array.Length - 1]] = dict_path[i];
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
        public void remove(SceneLable lable)
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
            foreach (var lable in lable_list)
            {
                lable_list_str += ",\n\t" + lable.ToString();
            }
            string all = "lable_name:" + lable_name + ",parent_lable_name:" + parent_lable_name +
                 ",[" + lable_list_str + "]";

            return all;
        }
    }

}