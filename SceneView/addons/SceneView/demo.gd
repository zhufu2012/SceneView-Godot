# =====================================================================
# 名称： GDEditor_V2
# 描述： 提供 Godot 插件开发相关的工具函数，加快插件开发，可直接在您的插件项目中使用
# 作者： @巽星石 @张学徒 @timothyqiu 
# 基于： @张学徒 在2022年5月13日分享的代码改进，部分独门秘技来自@timothyqiu帮助
# Godot版本：3.5 
#
#           【请尽量确保您的Godot版本一致，否则可能出现未知错误】
#
# 最后修改时间：2022年9月25日16:51:06
# =====================================================================

tool
extends EditorPlugin
class_name GDEditor

var face = get_editor_interface()
var root = face.get_tree().root # 编辑器根视口

# =======================  基本的节点查找函数  ======================= 
# 通过层级索引查找并返回节点
func get_child_by_indexPath(node:Node,indexPath:PoolIntArray):
	for idx in indexPath:
		var nd = node.get_child(idx)
		node = nd
	return node	

# 返回对应类名的子节点
func get_child_as_class(node:Node,className:String):
	var children = node.get_children()
	for child in children:
		if child.is_class(className):
			return child

# ======================= 编辑器部位的获取 =======================
# 整个顶部（编辑器菜单，视图切换，运行场景）部分的HBoxContainer容器
func top_container():
	var editor_panel = face.get_base_control()  # 编辑器的基础节点
	var main_vbox = get_child_as_class(editor_panel,"VBoxContainer")
	var top_container = get_child_as_class(main_vbox,"HBoxContainer") 
	return root.find_node("Scene",true,false) 

# 将控件添加到 - 编辑器右上角 - 运行场景等按钮之后
func add_control_to_editor_topright_area(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_TOOLBAR,control)

# 编辑顶部菜单的HBoxContainer容器
func top_menu_container():
	return top_container().get_child(0)
# ================================================ 顶部菜单

# 返回 - 索引为idx的顶部菜单 - MenuButton类型
func top_menu(idx:int):
	top_menu_container().get_child(idx)

# 添加自定义顶部菜单
func add_sys_top_menu(text:String,items:PoolStringArray,target:Object,itm_pressed_fuc:String):
	var top = top_menu_container()
	# 创建MenuButton
	var menu = MenuButton.new()
	menu.text = text # 名
	menu.get_popup().connect("id_pressed",target,itm_pressed_fuc) # 绑定菜单项点击
	# 添加菜单项
	var idx = 0
	for item in items:
		menu.get_popup().add_item(item,idx)
		idx += 1
	# 添加菜单
	top.add_child(menu)
	return menu.get_popup() # 返回PopupMenu


# 移除顶部菜单
# @param idx 要删除的顶部菜单在整个sys_top_menu_container中索引位置
#            一般请传入大于等于5的值，否则无效，不会删除原系统的菜单
func remove_sys_top_menu(idx = -1 ):
	var top = top_menu_container()
	var top_menus = top.get_children()
	if idx > 4 and idx < top_menus.size(): # 不删除原系统菜单
		top.remove_child(top_menus[idx])
	elif idx == -1: # 默认值，删除全部自定义菜单
		for aa in range(5,top.get_child_count()):
			top.remove_child(top_menus[aa])


# ============================================== 系统对话框
# 系统对话框
enum SYS_DLG{
	PROJECT_SETTING, # 项目设置
	EDITOR_SETTING,# 编辑器设置
	CREATE,# 新建资源
	PLUGIN_CONFIG,# 创建插件
	ABOUT,#关于Godot
	FEATURE_PROFILE_MANAGER,# 编辑器功能管理
	EXPORT# 项目导出对话框
}
# 显示对应的系统对话框
func show_sys_dialog(dlg:int,tab_index:int = 0):
	var pal = face.get_base_control()  # 编辑器的基础节点
	var className = [
		"ProjectSettingsEditor",
		"EditorSettingsDialog", 
		"CreateDialog", 
		"PluginConfigDialog", 
		"EditorAbout", 
		"EditorFeatureProfileManager", 
		"ProjectExportDialog" 
	]
	var _dlg = get_child_as_class(pal,className[dlg])
	# 选项卡
	var _tabs:TabContainer = get_child_as_class(_dlg,"TabContainer") 
	# 显示对话框
	_dlg.popup()
	# 切换选项卡
	if tab_index>0 and tab_index< _tabs.get_tab_count():
		_tabs.current_tab = tab_index
	else:
		_tabs.current_tab = 0

# ================================================ "场景"面板
func scene_tree_dock():
	return root.find_node("Scene",true,false) 

# ---- 节点添加 ----
# 返回当前场景中选中的节点
func get_selected_node() -> Node:
	var sels = face.get_selection().get_selected_nodes() # 获取当前选中的节点集合
	var _sel
	if sels.size() == 1:
		_sel =sels[0]
	else:
		_sel = null
	return _sel

# “添加节点"对话框
func create_node_dialog():
	return get_child_as_class(scene_tree_dock(),"CreateDialog")

# 当前正在编辑的场景的根节点
func edited_scene_root():
	return face.get_edited_scene_root()

# 判断当前场景是否为空场景(也就是没有添加任何节点)
func current_scene_is_empty() -> bool:
	if not edited_scene_root():# 当前场景为空场景
		return true
	else:
		return false

# 为空场景添加根节点
# @param nodeType 节点类型的名称，字符串 
func empty_scene_add_root(nodeType:String) -> void:	
	if current_scene_is_empty():# 当前场景为空场景
		# 新建根节点
		scene_tree_dock()._tool_selected(0) # 打开“添加Node”对话框 - @timothyqiu 提供的秘技
		# 搜索框
		var hs = create_node_dialog().get_child(3)
		var vb = hs.get_child(1)
		var mg = vb.get_child(1)
		var txt:LineEdit = mg.get_child(0).get_child(0)
		# "创建"按钮
		var hb = create_node_dialog().get_child(2)
		var creBtn:Button = hb.get_child(1)
		txt.text = nodeType # 修改搜索关键字
		txt.emit_signal("text_changed",nodeType) # 触发“文本改变”信号
		creBtn.emit_signal("pressed") # 触发“创建”按钮pressed信号


# 为当前场景选中节点添加对应类型的子节点
# @param nodeType 节点类型名称，字符串
func select_node_add_child_node(nodeType:String):
	var selected_node = get_selected_node()
	if selected_node: # 场景非空，存在选中节点
		if ClassDB.class_exists(nodeType):
			# 添加子节点
			var node:Node = ClassDB.instance(nodeType) # 按照类型名称添加节点
			selected_node.add_child(node)
			node.owner = edited_scene_root() # 设置owner为场景根节点

# ================================================ “文件系统”面板
func file_system_dock():	
	return face.get_file_system_dock()

# 在“文件系统”面板中选中路径为file_path的文件
func select_file(file_path:String) -> void:
	face.select_file(file_path)

# ================================================ “检视器”面板
func inspector_dock():	
	return face.get_inspector()

func inspect_object(object:Object,for_property:String,inspector_only:bool = false) -> void:
	face.inspect_object(object,for_property,inspector_only)
	
# 将控件添加到 - 检视器面板 - 底部
func add_control_to_inspector_bottom(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_PROPERTY_EDITOR_BOTTOM,control)



# 切换编辑器 2D、3D、Script、AssetLib或其他的屏幕插件名称
func set_main_screen_editor(name:String) -> void:
	face.set_main_screen_editor(name)

# === 场景操作相关 === 
func is_playing_scene() -> bool:
	return face.is_playing_scene()

func stop_playing_scene() -> void:
	face.stop_playing_scene()

func open_scene_from_path(scene_filepath:String) -> void:
	face.open_scene_from_path(scene_filepath)

func play_current_scene() -> void:
	face.play_current_scene()

func play_custom_scene(scene_file_path:String) -> void:
	face.play_custom_scene(scene_file_path)

func play_main_scene() -> void:
	face.play_main_scene()

func reload_scene_from_path(scene_file_path:String) -> void:
	face.reload_scene_from_path(scene_file_path)

func save_scene():
	return face.save_scene()

func save_scene_as(path:String,with_preview:bool = true) -> void:
	face.save_scene_as(path,with_preview)

# ==== 插件的启用和停止 ====
func is_plugin_enabled(plugin:String) -> bool:
	return face.is_plugin_enabled(plugin)

func set_plugin_enabled(plugin:String,enabled:bool) -> void:
	face.set_plugin_enabled(plugin,enabled)


## ================================================ 脚本编辑器
# ScriptEditor
func script_editor():
	return face.get_script_editor()
	
# 脚本菜单工具按钮列表（其中有部分不是按钮）
func script_top_container():
	return script_editor().get_child(0).get_child(0)

# “创建脚本”对话框
func script_create_dialog():
	return get_script_create_dialog()

# ScriptEditor下的TabContainer
func script_TabContainer():
	return get_child_by_indexPath(script_editor(),[0,1,1])

# 返回当前打开的脚本编辑器或内置文档对应的控件
func get_current_Script_tab_control():
	var sc_tabs:TabContainer = script_TabContainer() # 代码编辑器选项卡容器
	var index = sc_tabs.current_tab # 当前打开的选项卡索引值
	return sc_tabs.get_child(index)

# 返回当前打开的脚本编辑器的TextEdit控件引用
func get_current_Script_TextEdit():
	var ctl = get_current_Script_tab_control()
	if ctl.get_class() == "ScriptTextEditor":
		var sc_edt = ctl
		var txt_edt:TextEdit = get_child_by_indexPath(sc_edt,[0,0,0])
		return txt_edt
		
# 获取当前脚本TextEdit控件的右键菜单
func get_current_Script_PopupMenu(): # -> PopupMenu
	var ctl = get_current_Script_tab_control()
	return get_child_as_class(ctl,"PopupMenu")


# ---- 内置文档 ----
# 在脚本界面，打开相应className对应的类型的内置文档
func open_class_help_in_editor(className:String) -> void:
	if ClassDB.class_exists(className):
		set_main_screen_editor("Script") # 切换到脚本编辑器界面
		# 显示内置文档
		script_editor()._help_class_goto(className) # 同样来自 @timothyqiu 提供的秘技
		
# 关闭所有打开的内置文档
func close_all_EditorHelp() -> void:
	var sc_tabs:TabContainer = script_TabContainer() # 代码编辑器选项卡容器
	var ctls = sc_tabs.get_children()
	for ctl in ctls:
		if ctl.get_class() == "EditorHelp":
			sc_tabs.remove_child(ctl)

# ---- 插入代码 ----
# 在光标位置添加代码
func insert_code(code:String):
	var edt = get_current_Script_TextEdit()
	edt.insert_text_at_cursor(code)

# 追加内容到当前脚本编辑器末尾
func append_code(code:String):
	var edt = get_current_Script_TextEdit()
	edt.text += "\n\n" + code
	edt.cursor_set_line(edt.get_line_count()) # 定位到最后一行

# 追加文件中的代码到当前脚本编辑器末尾
func append_file_code(codePath:String):
	var dir = Directory.new()
	if dir.file_exists(codePath):
		append_code(loadString(codePath))

# ---- 插入注释 ----
# 分割线注释
func insert_HR_comment(str_or_char:String,repeat_time:int,comment:String = ""):
	repeat_time = clamp(repeat_time,0,15)
	insert_code("# " + str_or_char.repeat(repeat_time) + comment + str_or_char.repeat(repeat_time))

# 在脚本的最顶部添加元信息注释
func insert_top_meta_comment():
	var meta_comment = "# %s\n" % "=".repeat(70)
	meta_comment +=  "# 名称：\n"
	meta_comment +=  "# 类型：\n"
	meta_comment +=  "# 作者：\n"
	meta_comment +=  "# 创建时间：\n"
	meta_comment +=  "# 最后修改时间：\n"
	meta_comment += "# %s\n" % "=".repeat(70)
	var edt = get_current_Script_TextEdit()
	edt.text = meta_comment +"\n" + edt.text

func insert_param_comment():
	insert_code("# @param")
	
func insert_return_comment():
	insert_code("# @return")

# 关闭所有打开的脚本（不关闭已经打开的内置文档）
func close_all_script() -> void:
	var sc_tabs:TabContainer = script_TabContainer() # 代码编辑器选项卡容器
	var ctls = sc_tabs.get_children()
	for ctl in ctls:
		if ctl.get_class() == "ScriptTextEditor":
			sc_tabs.remove_child(ctl)

# === 2D场景 === 

# 将控件添加到 - 2D场景 - 顶部菜单
func add_control_to_2D_menu(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_CANVAS_EDITOR_MENU,control)
	
# 将控件添加到 - 2D场景 - 左侧
func add_control_to_2D_left(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_CANVAS_EDITOR_SIDE_LEFT,control)
	
# 将控件添加到 - 2D场景 - 右侧
func add_control_to_2D_right(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_CANVAS_EDITOR_SIDE_RIGHT,control)

# 将控件添加到 - 2D场景 - 底部
func add_control_to_2D_bottom(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_CANVAS_EDITOR_BOTTOM,control)

# === 3D场景 === 

# 将控件添加到 - 3D场景 - 顶部菜单
func add_control_to_3D_menu(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_MENU,control)

# 将控件添加到 - 3D场景 - 左侧
func add_control_to_3D_left(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_SIDE_LEFT,control)
	
# 将控件添加到 - 3D场景 - 右侧
func add_control_to_3D_right(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_SIDE_RIGHT,control)

# 将控件添加到 - 3D场景 - 底部
func add_control_to_3D_bottom(control:Control):
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_BOTTOM,control)



# ========================================== 额外的方法
# 返回指定路径文件中的内容
func loadString(path:String) -> String:
	var file = File.new()
	var string = ""
	file.open(path,File.READ)
	string = file.get_as_text() # 整个文件内容
	file.close()
	return string
	
