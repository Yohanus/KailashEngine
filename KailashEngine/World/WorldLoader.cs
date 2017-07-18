﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

using MuffinEngine.Physics;
using MuffinEngine.World.Model;
using MuffinEngine.World.Lights;

namespace MuffinEngine.World
{
    class WorldLoader
    {


        private string _path_scene;

        private Mesh _sLight_mesh;
        public Mesh sLight_mesh
        {
            get { return _sLight_mesh; }
        }

        private Mesh _pLight_mesh;
        public Mesh pLight_mesh
        {
            get { return _pLight_mesh; }
        }

        private PhysicsWorld _physics_world;

        private MaterialManager _material_manager;

        public WorldLoader(string path_scene, string light_objects_filename, PhysicsWorld physics_world, MaterialManager material_manager)
        {
            // Fill Base Paths
            _path_scene = path_scene;

            // Assign physics world
            _physics_world = physics_world;

            // Assign a material manager
            _material_manager = material_manager;

            // Load standard light object meshes
            Dictionary<string, UniqueMesh> light_objects = new Dictionary<string, UniqueMesh>();
            Dictionary<string, LightLoader.LightLoaderExtras> garbage_extras = new Dictionary<string, LightLoader.LightLoaderExtras>();
            try
            {
                DAE_Loader.load(
                    _path_scene + light_objects_filename + "/" + light_objects_filename + ".dae",
                    _material_manager, 
                    out light_objects,
                    out garbage_extras);
                _sLight_mesh = light_objects["sLight"].mesh;
                _pLight_mesh = light_objects["pLight"].mesh;
                light_objects.Clear();
                garbage_extras.Clear();
            }
            catch (Exception e)
            {
                Debug.DebugHelper.logError("[ ERROR ] Loading World File: " + light_objects_filename, e.Message);
            }
        }

        private string[] createFilePaths(string filename)
        {
            string mesh_filename = _path_scene + filename + "/" + filename + ".dae";
            string physics_filename = _path_scene + filename + "/" + filename + ".physics";
            string lights_filename = _path_scene + filename + "/" + filename + ".lights";

            return new string[]
            {
                mesh_filename,
                physics_filename,
                lights_filename
            };
        }

        public void createWorld(string filename, out List<UniqueMesh> meshes, out List<Light> lights)
        {
            Debug.DebugHelper.logInfo(1, "Loading World", filename);

            // Build filenames
            string[] filepaths = createFilePaths(filename);
            string mesh_filename = filepaths[0];
            string physics_filename = filepaths[1];
            string lights_filename = filepaths[2];


            Dictionary<string, UniqueMesh> temp_meshes;
            Dictionary<string, LightLoader.LightLoaderExtras> light_extras;

            DAE_Loader.load(
                mesh_filename,
                _material_manager,
                out temp_meshes, 
                out light_extras);
            lights = LightLoader.load(lights_filename, light_extras, _sLight_mesh, _pLight_mesh);
            PhysicsLoader.load(physics_filename, _physics_world, temp_meshes);
            meshes = temp_meshes.Values.ToList();

            temp_meshes.Clear();
            light_extras.Clear();

            Debug.DebugHelper.logInfo(1, "", "");
        }


        public void addWorldToScene(string[] filenames, List<UniqueMesh> meshes, LightManager light_manager)
        {
            List<UniqueMesh> temp_meshes;
            List<Light> temp_lights;

            foreach (string filename in filenames)
            {
                try
                {
                    createWorld(filename, out temp_meshes, out temp_lights);

                    meshes.AddRange(temp_meshes);
                    light_manager.addLight(temp_lights);

                    temp_meshes.Clear();
                    temp_lights.Clear();
                }
                catch (Exception e)
                {
                    Debug.DebugHelper.logError("[ ERROR ] Loading World File: " + filename, e.Message);
                }
            }
        }



    }
}
