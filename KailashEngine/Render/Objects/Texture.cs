﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MuffinEngine.Render.Objects
{
    class Texture
    {
        //------------------------------------------------------
        // IDs
        //------------------------------------------------------

        private int _id;
        public int id
        {
            get { return _id; }

        }

        private TextureTarget _target;
        public TextureTarget target
        {
            get { return _target; }
        }

        private long _handle;
        public long handle
        {
            get { return _handle; }
        }


        //------------------------------------------------------
        // Options
        //------------------------------------------------------
        private bool _enable_mipmap;
        public bool enable_mipmap
        {
            get { return _enable_mipmap; }
            set { _enable_mipmap = value; }
        }

        private int _max_mipmap_levels;

        private bool _enable_aniso;
        public bool enable_aniso
        {
            get { return _enable_aniso; }
            set { _enable_aniso = value; }
        }

        private float _max_anisotropy;

        private bool _bindless = false;
        public bool bindless
        {
            get
            {
                return _bindless;
            }
        }

        //------------------------------------------------------
        // Pixel Settings
        //------------------------------------------------------

        private PixelInternalFormat _pif;
        public PixelInternalFormat pif
        {
            get { return _pif; }
        }

        private PixelFormat _pf;
        public PixelFormat pf
        {
            get { return _pf; }
        }

        private PixelType _pt;
        public PixelType pt
        {
            get { return _pt; }
        }


        //------------------------------------------------------
        // Sizing
        //------------------------------------------------------

        private int _width;
        public int width
        {
            get { return _width; }
        }

        private int _height;
        public int height
        {
            get { return _height; }
        }

        private int _depth;
        public int depth
        {
            get { return _depth; }
        }

        public Vector3 dimensions
        {
            get
            {
                return new Vector3(_width, _height, _depth);
            }
        }

        //------------------------------------------------------
        // Texture Parameters
        //------------------------------------------------------

        private TextureMinFilter _min_filter;
        public TextureMinFilter min_filter
        {
            get { return _min_filter; }
        }

        private TextureMagFilter _mag_filter;
        public TextureMagFilter mag_filter
        {
            get { return _mag_filter; }
        }

        private TextureWrapMode _wrap_mode;
        public TextureWrapMode wrap_mode
        {
            get { return _wrap_mode; }
        }

        private Vector4 _border_color;
        public Vector4 border_color
        {
            get { return _border_color; }
        }


        //------------------------------------------------------
        // Constructor
        //------------------------------------------------------

        public Texture(TextureTarget target,
            int width, int height, int depth,
            bool enable_mipmap, bool enable_aniso,
            PixelInternalFormat pif, PixelFormat pf, PixelType pt,
            TextureMinFilter min_filter, TextureMagFilter mag_filter, TextureWrapMode wrap_mode)
            : this(target, width, height, depth, enable_mipmap, enable_aniso,  pif, pf, pt, min_filter, mag_filter, wrap_mode, Vector4.Zero)
        { }

        public Texture(TextureTarget target,
            int width, int height, int depth,
            bool enable_mipmap, bool enable_aniso,
            PixelInternalFormat pif, PixelFormat pf, PixelType pt,
            TextureMinFilter min_filter, TextureMagFilter mag_filter, TextureWrapMode wrap_mode, Vector4 border_color)
        {
            // Set texture configuration
            _target = target;

            _width = width;
            _height = height;
            _depth = (target == TextureTarget.TextureCubeMap) ? 6 : depth;

            _enable_mipmap = enable_mipmap;
            _enable_aniso = enable_aniso;

            _pif = pif;
            _pf = pf;
            _pt = pt;

            _min_filter = min_filter;
            _mag_filter = mag_filter;
            _wrap_mode = wrap_mode;

            _border_color = border_color;

            if(_enable_mipmap)
            {
                _max_mipmap_levels = getMaxMipMap(_width, _height);
                _min_filter = TextureMinFilter.LinearMipmapLinear;
            }

            if(_enable_aniso)
            {
                _max_anisotropy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropyExt);
            }

            // Finally, Generate a texture object
            GL.GenTextures(1, out _id);

        }


        //------------------------------------------------------
        // Helpers
        //------------------------------------------------------

        public static int getMaxMipMap(int width, int height)
        {
            int largest_dimension = Math.Max(width, height);

            return (int)Math.Log(largest_dimension, 2) - 1;
        }

        public int getMaxMipMap()
        {
            return getMaxMipMap(_width, _height);
        }

        public void setMaxMipMap(int max_mipmap_levels)
        {
            _max_mipmap_levels = Math.Min(getMaxMipMap(_width, _height), max_mipmap_levels);
            _min_filter = TextureMinFilter.LinearMipmapLinear;
        }

        public void generateMipMap()
        {
            GL.GenerateTextureMipmap(_id);
        }


        //------------------------------------------------------
        // Loading
        //------------------------------------------------------

        private void setTextureParameters()
        {

            GL.TexParameter(
                _target,
                TextureParameterName.TextureMinFilter,
                (float)_min_filter);
            GL.TexParameter(
                _target,
                TextureParameterName.TextureMagFilter,
                (float)_mag_filter);
            GL.TexParameter(
                _target,
                TextureParameterName.TextureWrapS,
                (float)_wrap_mode);
            GL.TexParameter(
                _target,
                TextureParameterName.TextureWrapT,
                (float)_wrap_mode);
            GL.TexParameter(
                _target,
                TextureParameterName.TextureWrapR,
                (float)_wrap_mode);

            if(_border_color != Vector4.Zero)
            {
                GL.TexParameter(
                    _target,
                    TextureParameterName.TextureBorderColor,
                    EngineHelper.createArray(_border_color));
            }

            if (_enable_mipmap)
            {
                GL.TexParameter(_target, TextureParameterName.TextureBaseLevel, 0);
                GL.TexParameter(_target, TextureParameterName.TextureMaxLevel, _max_mipmap_levels);
                generateMipMap();
            }

            if (_enable_aniso)
            {
                GL.TexParameter(_target, (TextureParameterName)All.TextureMaxAnisotropyExt, _max_anisotropy);
            }
        }

        public void load()
        {
            load((IntPtr)0);
        }

        public void load(IntPtr data)
        {

            GL.BindTexture(_target, _id);

            switch (_target)
            {
                case TextureTarget.Texture1D:
                    GL.TexImage1D(_target, 0, _pif, _width, 0, _pf, _pt, data);
                    break;
                case TextureTarget.Texture2D:
                    GL.TexImage2D(_target, 0, _pif, _width, _height, 0, _pf, _pt, data);
                    break;
                case TextureTarget.Texture2DArray:
                    GL.TexImage3D(_target, 0, _pif, _width, _height, _depth, 0, _pf, _pt, (IntPtr)0);
                    break;
                case TextureTarget.TextureCubeMap:
                    for (int face = 0; face < _depth; face++)
                    {
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, _pif, _width, _height, 0, _pf, _pt, data);
                    }
                    break;
                case TextureTarget.TextureCubeMapArray:
                    //GL.TexImage3D(_target, 0, pif, _width, _depth, _depth * 6, 0, _pf, _pt, data);
                    GL.TexStorage3D((TextureTarget3d)_target, _max_mipmap_levels + 1, (SizedInternalFormat)_pif, _width, _height, _depth * 6);
                    //for (int layer = 0; layer < _depth; layer++)
                    //{
                    //    for (int face = 0; face < 6; face++)
                    //    {
                    //        GL.TexSubImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, 0, 0, _width, _height, _pf, _pt, data);
                    //        //GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, layer, _pif, _width, _height, 0, _pf, _pt, data);
                    //    }
                    //}
                    break;
                case TextureTarget.Texture3D:
                    GL.TexImage3D(_target, 0, _pif, _width, _height, _depth, 0, _pf, _pt, data);
                    break;
                default:
                    throw new Exception($"Load Texture: Unsupported Texture Target [ {_target.ToString()} ]");
            }

            setTextureParameters();

        }

        public void load(IntPtr[] data)
        {
            if(data.Length != _depth)
            {
                throw new Exception("Load Texture: Length of data array does not match texture's depth");
            }

            GL.BindTexture(_target, _id);

            switch (_target)
            {
                case TextureTarget.Texture1D:
                    throw new Exception("Load Texture: Cannot load data array into Texture1D");
                case TextureTarget.Texture2D:
                    throw new Exception("Load Texture: Cannot load data array into Texture2D");
                case TextureTarget.Texture2DArray:
                    GL.TexImage3D(_target, 0, _pif, _width, _height, _depth, 0, _pf, _pt, (IntPtr)0);
                    for (int slice = 0; slice < _depth; slice++)
                    {
                        GL.TexSubImage3D(_target, 0, 0, 0, slice, _width, _height, 1, _pf, _pt, data[slice]);
                    }
                    break;
                case TextureTarget.TextureCubeMap:
                    for (int face = 0; face < 6; face++)
                    {
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, _pif, _width, _height, 0, _pf, _pt, data[face]);
                    }
                    break;
                case TextureTarget.Texture3D:
                    GL.TexImage3D(_target, 0, _pif, _width, _height, _depth, 0, _pf, _pt, (IntPtr)0);
                    for (int slice = 0; slice < _depth; slice++)
                    {
                        GL.TexSubImage3D(_target, 0, 0, 0, slice, _width, _height, 1, _pf, _pt, data[slice]);
                    }
                    break;
                default:
                    throw new Exception($"Load Texture: Unsupported Texture Target [ {_target.ToString()} ]");
            }

            setTextureParameters();
        }


        public void loadBindless()
        {
            _handle = GL.Arb.GetTextureHandle(_id);
            _bindless = true;
        }


        //------------------------------------------------------
        // Unloading
        //------------------------------------------------------
        public void clear()
        {
            Vector4 clear_value = new Vector4(0.0f);
            for(int i = 0; i < _max_mipmap_levels + 1; i++)
            {
                GL.ClearTexImage(_id, i, _pf, _pt, ref clear_value);
            }
        }


        //------------------------------------------------------
        // Binding
        //------------------------------------------------------

        public void bind(int texture_uniform, int index)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + index);
            GL.BindTexture(_target, _id);
            GL.Uniform1(texture_uniform, index);
        }

        public void bindImageUnit(int texture_uniform, int index, TextureAccess access)
        {
            bindImageUnit(texture_uniform, index, access, 0, 0);
        }

        public void bindImageUnit(int texture_uniform, int index, TextureAccess access, int level)
        {
            bindImageUnit(texture_uniform, index, access, level, 0);
        }

        public void bindImageUnit(int texture_uniform, int index, TextureAccess access, int level, int layer)
        {
            bool layered = false;
            switch (_target)
            {
                case TextureTarget.Texture2DArray:
                case TextureTarget.TextureCubeMap:
                case TextureTarget.TextureCubeMapArray:
                case TextureTarget.Texture3D:
                    layered = true;
                    break;
            }

            GL.ActiveTexture(TextureUnit.Texture0 + index);
            GL.BindImageTexture(
                index,
                _id,
                level,
                layered,
                layer,
                access,
                (SizedInternalFormat)_pif);
            GL.Uniform1(texture_uniform, index);
        }

        public void makeResident()
        {
            GL.Arb.MakeTextureHandleResident(_handle);
        }

        public void makeNonResident()
        {
            GL.Arb.MakeTextureHandleNonResident(_handle);
        }

    }
}
