using Godot;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace SceneCore_Space
{
    //保存加载 标签及场景数据
    public class SaveLoadData
    {
        //根节点
        public static SceneLable parent_lable;
        public string path = "res://addons/SceneView/data.json";
        public SaveLoadData()
        {
            //读取标签及场景数据
            FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            
            parent_lable = JsonSerializer.Deserialize<SceneLable>(file.GetAsText(), new JsonSerializerOptions
            {
                WriteIndented = true,
                // 处理循环引用类型 给类对象加上id
                ReferenceHandler = ReferenceHandler.Preserve,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs, UnicodeRanges.CjkSymbolsandPunctuation)

            });
            file.Close();

            GD.Print("测试");
            GD.Print(parent_lable.ToString());
        }

        //获取反序列化后的标签及场景数据
        public SceneLable GetSceneLabelList()
        {
            return parent_lable;
        }




        public bool SaveData()
        {
            try
            {
                
                string json = JsonSerializer.Serialize(parent_lable, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.Preserve,
                    IncludeFields = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs, UnicodeRanges.CjkSymbolsandPunctuation)
                });

                //GD.Print("测试保存");
                //GD.Print(json);
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
                file.StoreString(json);

                return true;
            }
            catch (Exception e)
            {
                GD.Print(e.StackTrace);
                return false;
            }
        }



        //标签真实名称  添加的子标签
        public bool Add(string label_name, SceneLable label)
        {
            return true;

        }


    }
}