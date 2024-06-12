extends Tree

@onready var tree:Tree = $"."

func _ready():
	# 创建根节点
	var root:TreeItem = tree.create_item()
	root.set_text(0,"root")             # 设定根节点文本

	# 创建根节点的子节点
	var itm:TreeItem = tree.create_item(root)
	itm.set_text(0,"子节点1")
	var itm2:TreeItem = tree.create_item(root)
	itm2.set_text(0,"子节点2")
	var itm3:TreeItem = tree.create_item(itm2)
	itm3.set_text(0,"子节点2")


func _get_drag_data(at_position):
	if get_tree(): # 不为空
		var sel:TreeItem = get_item_at_position(at_position)
		if sel: # 有选中项
			set_drag_preview(make_drag_preview(sel))
			return [sel]

func _can_drop_data(at_position, data):
	# 如果拖放数据是一个TreeItem就可以放置
	# 拖放完毕恢复拖动标志设定
	drop_mode_flags = DROP_MODE_ON_ITEM | DROP_MODE_INBETWEEN
	return data.size() > 0 and (data[0] is TreeItem)

func _drop_data(at_position, data):
	# 获取目标拖放位置，-1,0,1分别代表在某项之前、之上和之后
	var target_pos = get_drop_section_at_position(at_position)
	# 获取鼠标位置处的TreeItem
	var target_itm:TreeItem = get_item_at_position(at_position)
	# 如果目标位置处TreeItem是data[0]的子孙节点
	if target_itm in get_items(data[0]):
		return # 禁止移动
	match target_pos:
		-1: # 拖放到了某个TreeItem之前
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
	if item.get_child_count()>0:
		arr.append_array(item.get_children())
		for chd in item.get_children():
			arr.append_array(get_items(chd))
	return arr

# 创建拖动预览
func make_drag_preview(itm:TreeItem) -> Button:
	var btn = Button.new()
	#btn.flat = true
	btn.text = itm.get_text(0)
	btn.icon = itm.get_icon(0)
	return btn

