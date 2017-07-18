﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;

using MuffinEngine.Render.Objects;

namespace MuffinEngine.Render
{
    class StaticImageLoader
    {

        private string _path_static_textures_base;
        public string path_static_textures_base
        {
            get { return _path_static_textures_base; }
            set { _path_static_textures_base = value; }
        }



        public StaticImageLoader(string static_textures_base_path)
        {
            _path_static_textures_base = static_textures_base_path;
        }


        public Image createImage(string static_texture_path, TextureTarget texture_target, TextureWrapMode wrap_mode, bool use_srgb = true)
        {
            Image temp_image = new Image(_path_static_textures_base + static_texture_path, use_srgb, texture_target, wrap_mode);
            temp_image.load();
            return temp_image;
        }

        public Image createImage(string[] files, TextureTarget texture_target, TextureWrapMode wrap_mode, bool use_srgb)
        {
            List<string> filepaths = new List<string>();
            foreach (string file in files)
            {
                filepaths.Add(_path_static_textures_base + file);
            }
            Image temp_image = new Image(filepaths.ToArray(), use_srgb, texture_target, wrap_mode, true, true);
            temp_image.load();

            filepaths.Clear();
            return temp_image;
        }

    }
}
