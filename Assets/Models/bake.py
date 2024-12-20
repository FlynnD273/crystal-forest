import platform
import bpy
from time import time
from datetime import timedelta
import os
import subprocess
import signal
import bmesh
from bpy.types import NodeTree, ShaderNode

bpy.ops.wm.save_mainfile()

is_shutdown = False


def shutdown() -> None:
    global is_shutdown
    is_shutdown = True


signal.signal(signal.SIGINT, lambda _, b: shutdown())

tex_size = 256

output_dir = os.path.join(bpy.path.abspath("//"), "Textures")
if not os.path.exists(output_dir):
    os.mkdir(output_dir)

bpy.context.scene.render.engine = "CYCLES"
bpy.context.scene.cycles.device = "GPU"

objs = [o for o in bpy.context.visible_objects if o.type == "MESH"]


def get_inner_shader(tree) -> tuple[NodeTree, ShaderNode] | tuple[None, None]:
    prince = tree.nodes.get("Principled BSDF")
    if prince is not None:
        return (tree, prince)
    groups = get_groups(tree)

    while len(groups) > 0:
        grp = groups.pop(0)
        prince = grp.node_tree.nodes.get("Principled BSDF")
        if prince is not None:
            return (grp.node_tree, prince)

        new_grps = get_groups(grp.node_tree)
        for g in new_grps:
            groups.append(g)

    return (None, None)


def get_groups(node_tree):
    groups = []
    for node in node_tree.nodes:
        if node.type == "GROUP":
            groups.append(node)
    return groups


def maprange(val, low, high, new_low, new_high, clamp=True):
    if high == low or new_high == new_low:
        return new_high
    newval = (val - low) / (high - low) * (new_high - new_low) + new_low
    if clamp:
        newval = min(max(newval, new_low), new_high)
    return newval


def get_surface_area(obj):
    if obj.type != "MESH":
        print(f"The object {obj.name} is not a mesh.")
        return 0

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    surface_area = sum(f.calc_area() for f in bm.faces)

    bm.free()
    return surface_area


uvmap = "bakedUV"


def process_obj(obj):
    # Select only obj
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.make_single_user(
        object=True, obdata=True, material=True, animation=True, obdata_animation=True
    )

    # Unwrap to new UV map
    obj.data.uv_layers.new(name=uvmap)
    obj.data.uv_layers.active = obj.data.uv_layers[uvmap]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.lightmap_pack()
    bpy.ops.object.mode_set(mode="OBJECT")


def obj_to_path(obj):
    img_name = obj.name + "-bake"
    return os.path.join(output_dir, img_name + ".png")


def convert_attributes_to_vertex_groups(obj):
    if obj.type != "MESH":
        return

    mesh = obj.data
    attributes = mesh.attributes
    if uvmap in attributes:
        attributes.remove(attributes[uvmap])

    for attr in attributes:
        if (
            attr.domain != "POINT"
            or attr.data_type not in ["FLOAT", "BOOLEAN", "INT"]
            or attr.name[0] == "."
            or attr.name[0] == attr.name[0].lower()
            or "UV" in attr.name
        ):
            continue

        vertex_group = obj.vertex_groups.get(attr.name)
        if not vertex_group:
            vertex_group = obj.vertex_groups.new(name=attr.name)

        for i, data in enumerate(attr.data):
            if attr.data_type == "BOOLEAN":
                weight = float(data.value)
            elif attr.data_type == "INT":
                weight = min(max(float(data.value), 0.0), 1.0)
            else:
                weight = data.value

            vertex_group.add([i], weight, "REPLACE")


obj_scl = [get_surface_area(o) for o in objs]
max_surf = max(obj_scl)
obj_scl = [s / max_surf for s in obj_scl]
sorted_scl = obj_scl.copy()
sorted_scl.sort()
min_val = sorted_scl[int(len(sorted_scl) * 0.25)]
max_val = sorted_scl[int(len(sorted_scl) * 0.75)]
obj_scl = [maprange(s, min_val, max_val, 0.5, 1) for s in obj_scl]

problems = []
to_process = []

start_time = time()
for i, obj in enumerate(objs):
    if is_shutdown:
        break
    avg = (time() - start_time) / (i + 1)
    print(
        f"\rBaking {obj.name} | {i + 1}/{len(objs)} Time left: {timedelta(seconds= avg * (len(objs) - i))}{' '*10}"
    )

    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.make_single_user(
        object=True, obdata=True, material=True, animation=True, obdata_animation=True
    )

    for modifier in obj.modifiers:
        if "Armature" not in modifier.name:
            print("applying modifier", modifier.name)
            bpy.ops.object.modifier_apply(modifier=modifier.name)

    convert_attributes_to_vertex_groups(obj)

    for mat in obj.material_slots[:]:
        if not mat:
            print("Removing blank material")
            obj.material_slots.remove(mat)

    if obj.material_slots is None or len(obj.material_slots) == 0:
        print("Skipping", obj.name)
        continue

    if is_shutdown:
        break

    to_process.append(obj)
    diffuse_path = obj_to_path(obj)
    if os.path.exists(diffuse_path):
        continue

    process_obj(obj)

    img_name = obj.name + "-bake"
    scl_size = tex_size  # int(max(tex_size * obj_scl[i], 1))
    bpy.context.scene.render.resolution_x = scl_size
    bpy.context.scene.render.resolution_y = scl_size
    bpy.ops.image.new(name=img_name, width=scl_size, height=scl_size)
    bake_image = bpy.data.images[img_name]

    to_delete = []

    # Add the UV map and the image texture nodes to each material
    for slot in obj.material_slots:
        material = slot.material
        if material is None:
            continue

        tree, prince = get_inner_shader(material.node_tree)
        if tree is None or prince is None:
            problems.append(f"Object: {obj.name} Material: {material.name}")
            continue

        nodes = tree.nodes

        uv_map_node = nodes.new(type="ShaderNodeUVMap")
        uv_map_node.uv_map = uvmap
        uv_map_node.location = (-50, 0)
        uv_map_node.select = False

        texture_node = nodes.new(type="ShaderNodeTexImage")
        texture_node.image = bake_image

        tree.links.new(texture_node.inputs[0], uv_map_node.outputs["UV"])
        nodes.active = texture_node
        texture_node.select = False

        to_delete.append((nodes, uv_map_node))
        to_delete.append((nodes, texture_node))

        output_socket = nodes.get("Material Output").inputs[0]

        for link in tree.links:
            if link.to_socket == prince.inputs["Base Color"]:
                tree.links.new(link.from_socket, output_socket)

    # It's baking time
    bpy.context.scene.cycles.bake_type = "EMIT"
    bpy.context.scene.render.bake.use_pass_direct = False
    bpy.context.scene.render.bake.use_pass_indirect = False
    bpy.context.scene.render.bake.use_pass_color = True
    bpy.ops.object.bake(type="EMIT")
    bake_image.file_format = "PNG"
    bake_image.filepath_raw = diffuse_path
    bake_image.save()

    # for slot in obj.material_slots:
    #     material = slot.material
    #     if material is None:
    #         continue

    #     tree, prince = get_inner_shader(material.node_tree)
    #     if tree is None or prince is None:
    #         problems.append(f"Object: {obj.name} Material: {material.name}")
    #         continue

    #     nodes = tree.nodes

    #     output_socket = nodes.get("Material Output").inputs[0]

    #     has_alpha = False
    #     for link in tree.links:
    #         if link.to_socket == prince.inputs["Alpha"]:
    #             tree.links.new(link.from_socket, output_socket)
    #             has_alpha = True
    #             break
    #     if not has_alpha:
    #         rgb_node = tree.nodes.new(type="ShaderNodeRGB")
    #         rgb_node.outputs["Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    #         tree.links.new(rgb_node.outputs["Color"], output_socket)

    # bpy.ops.object.bake(type="EMIT")
    # alpha_path = os.path.join(output_dir, img_name + "_ALPHA.png")
    # bake_image.filepath_raw = alpha_path
    # bake_image.save()

    # magick_cmd = ["magick"]
    # cmd = magick_cmd + [
    #     alpha_path,
    #     "-colorspace",
    #     "gray",
    #     alpha_path,
    # ]
    # subprocess.run(cmd)
    # cmd = magick_cmd + [
    #     diffuse_path,
    #     alpha_path,
    #     "-alpha",
    #     "Off",
    #     "-compose",
    #     "CopyOpacity",
    #     "-composite",
    #     diffuse_path,
    # ]
    # subprocess.run(cmd)
    # os.remove(alpha_path)

    # for nodes, node in to_delete:
    #     nodes.remove(node)

    bpy.data.images[img_name].gl_free()
    bpy.data.images.remove(bake_image)
    bpy.ops.outliner.orphans_purge(
        do_local_ids=True, do_linked_ids=True, do_recursive=True
    )

if not is_shutdown:
    start_time = time()
    for i, obj in enumerate(to_process):
        avg = (time() - start_time) / (i + 1)
        print(
            f"\rProcessing {obj.name} | {i + 1}/{len(objs)} Time left: {timedelta(seconds= avg * (len(objs) - i))}{' '*10}"
        )
        idx = 0
        for _ in range(len(obj.data.uv_layers)):
            if obj.data.uv_layers[idx].name == "UV":
                idx += 1
            else:
                obj.data.uv_layers.remove(obj.data.uv_layers[idx])
        # for _ in range(1):
        #     bpy.ops.mesh.uv_texture_add()
        process_obj(obj)
        obj.data.materials.clear()
        img_name = obj.name + "-bake"
        bake_image = bpy.data.images.load(obj_to_path(obj))
        new_material = bpy.data.materials.new(name=img_name)
        obj.data.materials.append(new_material)
        new_material.use_nodes = True

        tree = new_material.node_tree
        nodes = tree.nodes

        prince = nodes.get("Principled BSDF")
        prince.location = (500, 0)

        # Add the UV map, image texture, and diffuse nodes
        output = tree.get_output_node("CYCLES")
        output.location = (700, 0)

        uv_map_node = nodes.new(type="ShaderNodeUVMap")
        uv_map_node.uv_map = uvmap
        uv_map_node.location = (-50, 0)
        uv_map_node.select = False

        texture_node = nodes.new(type="ShaderNodeTexImage")
        texture_node.image = bake_image
        texture_node.select = False
        texture_node.location = (150, 0)

        # Link everything together
        tree.links.new(texture_node.inputs[0], uv_map_node.outputs["UV"])
        tree.links.new(prince.inputs["Base Color"], texture_node.outputs[0])
        tree.links.new(prince.inputs["Alpha"], texture_node.outputs[1])
        tree.links.new(output.inputs[0], prince.outputs[0])

    print("Done")
    # Save the blend file as a copy
    folder = os.path.dirname(bpy.context.blend_data.filepath)
    name = os.path.splitext(os.path.basename(bpy.context.blend_data.filepath))[0]
    newname = "baked-" + name + ".blend"
    # bpy.ops.outliner.orphans_purge(
    #     do_local_ids=True, do_linked_ids=True, do_recursive=True
    # )
    # bpy.ops.wm.save_as_mainfile(filepath=os.path.join(folder, newname))

    # Export to OBJ
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.visible_objects:
        obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=os.path.join(folder, f"FBX-{name}.fbx"), use_visible=True
    )
    if len(problems) > 0:
        print("Could not parse materials:")
        for p in problems:
            print(p)
    bpy.ops.wm.revert_mainfile()

