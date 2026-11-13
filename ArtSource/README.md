# Art sources

Blender `.blend` files and other working files live here, **outside `Assets/`**, so Unity never tries to import them (that would require Blender on every machine).

Export finished meshes as `.fbx` into `Assets/Art/...`: scale 1, Apply Transform on, Y up, -Z forward. In Unity enable *Bake Axis Conversion* on import.
