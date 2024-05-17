#if TOOLS
using Godot;
using System;
using System.IO;
using System.Text.RegularExpressions;


namespace SceneCore_Space
{
    [Tool]
    public partial class SceneCore : EditorPlugin
    {
        //主界面
        private Control MainPanelInstance;
        private Label SceneName;

        public override void _EnterTree()
        {

            MainPanelInstance = GD.Load<PackedScene>("res://addons/SceneView/SceneView.tscn").Instantiate<Control>();

            IniView(false);

            AddControlToDock(DockSlot.LeftUl, MainPanelInstance);
            //注册 自动加载
            //AddAutoloadSingleton(AutoloadName, "res://addons/SceneView/SceneView.tscn");
        }


        public override void _ExitTree()
        {
            //反注册 自动加载
            //RemoveAutoloadSingleton(AutoloadName);
            if (MainPanelInstance != null)
            {
                RemoveControlFromDocks(MainPanelInstance);
                MainPanelInstance.Free();
            }
        }

        //初始化页面
        public void IniView(bool IsRefresh)
        {
            if (IsRefresh)
            {
                // 清空子节点
                foreach (Node child in MainPanelInstance.GetChildren())
                {
                    child.QueueFree();
                }
            }

            string[] sceneFiles = get_path();
            int i = 0;
            foreach (string sceneFile in sceneFiles)
            {
                string name = get_scene_name(sceneFile);//通过场景路径获取场景名称
                if (name != "")
                {
                    Control SceneNode = GD.Load<PackedScene>("res://addons/SceneView/Scene_node.tscn").Instantiate<Control>();
                    LineEdit scene_name = SceneNode.GetNode<LineEdit>("name");
                    scene_name.Text = name;
                    scene_name.Position = new Vector2(1, 0);
                    SceneNode.Position = new Vector2(0, i * scene_name.Size.Y + 5);
                    MainPanelInstance.AddChild(SceneNode);
                    i++;
                }
            }
            Button button = new Button();
            button.Text = "刷新场景";
            button.Size = new Vector2(80, 31);
            button.Position = new Vector2(250, 1);
            button.ButtonDown += OnButtonRefreshView;
            MainPanelInstance.AddChild(button);
        }

        // 按钮点击事件处理程序
        private void OnButtonRefreshView()
        {
            IniView(true);
        }

        //获取所有场景名称和路径
        public static string[] get_path()
        {
            string scenesFolder = "res://";
            string scenesFolderPath = ProjectSettings.GlobalizePath(scenesFolder);
            string[] sceneFiles = Directory.GetFiles(scenesFolderPath, "*.tscn", SearchOption.AllDirectories);
            return sceneFiles;
        }

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
#endif