﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using MuffinEngine.Client;
using MuffinEngine.Output;
using MuffinEngine.Render.Shader;
using MuffinEngine.Render.Objects;

using MuffinEngine.World;
using MuffinEngine.World.Lights;

namespace MuffinEngine.Render.FX
{
    class fx_gBuffer : RenderEffect
    {
        // Properties
        private bool _enable_Wireframe = false;

        // Programs
        private Program _pGeometry;
        private Program _pStencil;
        private Program _pLighting_SPOT;
        private Program _pLighting_POINT;
        private Program _pAccumulation;

        // Frame Buffers
        private FrameBuffer _fGBuffer;
        public FrameBuffer fGBuffer
        {
            get
            {
                return _fGBuffer;
            }
        }

        // Textures
        private Texture _tDepthStencil;
        public Texture tDepthStencil
        {
            get { return _tDepthStencil; }
        }

        private Texture _tDiffuse_ID;
        public Texture tDiffuse_ID
        {
            get { return _tDiffuse_ID; }
        }

        private Texture _tNormal_Depth;
        public Texture tNormal_Depth
        {
            get { return _tNormal_Depth; }
        }

        private Texture _tSpecular;
        public Texture tSpecular
        {
            get { return _tSpecular; }
        }

        private Texture _tVelocity;
        public Texture tVelocity
        {
            get { return _tVelocity; }
        }

        private Texture _tLighting_Diffuse;
        public Texture tLighting_Diffuse
        {
            get { return _tLighting_Diffuse; }
        }

        private Texture _tLighting_Specular;
        public Texture tLighting_Specular
        {
            get { return _tLighting_Specular; }
        }



        public fx_gBuffer(ProgramLoader pLoader, StaticImageLoader tLoader, string glsl_effect_path, Resolution full_resolution)
            : base(pLoader, tLoader, glsl_effect_path, full_resolution)
        { }


        protected override void load_Programs()
        {
            string[] geometry_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_cameraSpatials,
                EngineHelper.path_glsl_common_ubo_bindlessTextures_Materials,
                _path_glsl_effect + "helpers/gBuffer_Functions.include",
                EngineHelper.path_glsl_common_helper_linearDepth

            };
            string[] geometry_extensions = new string[]
            {
                EngineHelper.path_glsl_common_ext_bindlessTextures
            };
            string[] geom_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_cameraSpatials

            };
            string[] tesc_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_cameraSpatials,
                EngineHelper.path_glsl_common_helper_culling

            };
            string[] tese_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_bindlessTextures_Materials
            };


            string[] stencil_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_cameraSpatials
            };


            string[] lighting_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_cameraSpatials,
                EngineHelper.path_glsl_common_helper_lightingFunctions,
                EngineHelper.path_glsl_common_helper_positionFromDepth,
                EngineHelper.path_glsl_common_helper_shadowEvaluation,
                EngineHelper.path_glsl_common_helper_linearDepth
            };
            string[] spot_lighting_helpers = new string[]
            {
                EngineHelper.path_glsl_common_ubo_shadowMatrices_Spot
            };
            spot_lighting_helpers = spot_lighting_helpers.Concat(lighting_helpers).ToArray();
            string[] point_lighting_helpers = lighting_helpers;

            // Rendering Geometry into gBuffer
            _pGeometry = _pLoader.createProgram_Geometry(new ShaderFile[]
            {
                new ShaderFile(ShaderType.TessControlShader, _path_glsl_effect + "gBuffer_Geometry.tesc", tesc_helpers),
                new ShaderFile(ShaderType.TessEvaluationShader, _path_glsl_effect + "gBuffer_Geometry.tese", tese_helpers, geometry_extensions),
                new ShaderFile(ShaderType.GeometryShader, _path_glsl_effect + "gBuffer_Geometry.geom", geom_helpers),
                new ShaderFile(ShaderType.FragmentShader, _path_glsl_effect + "gBuffer_Geometry.frag", geometry_helpers, geometry_extensions)
            });
            _pGeometry.enable_MeshLoading();
            _pGeometry.addUniform("enable_Wireframe");
            _pGeometry.addUniform("render_size");

            // Stencil light bounds for lighting pass
            _pStencil = _pLoader.createProgram(new ShaderFile[]
            {
                new ShaderFile(ShaderType.VertexShader, _path_glsl_effect + "gBuffer_Stencil.vert", stencil_helpers)
            });
            _pStencil.addUniform(RenderHelper.uModel);

            // Calculate Lighting for Spot Lights
            _pLighting_SPOT = _pLoader.createProgram(new ShaderFile[]
            {
                new ShaderFile(ShaderType.VertexShader, _path_glsl_effect + "gBuffer_Stencil.vert", stencil_helpers),
                new ShaderFile(ShaderType.FragmentShader, _path_glsl_effect + "gBuffer_Lighting_SPOT.frag", spot_lighting_helpers)
            });
            _pLighting_SPOT.addUniform(RenderHelper.uModel);
            _pLighting_SPOT.enable_LightCalculation();
            _pLighting_SPOT.enable_Samplers(3);
            _pLighting_SPOT.addUniform("shadow_id");

            // Calculate Lighting for Point Lights
            _pLighting_POINT = _pLoader.createProgram(new ShaderFile[]
            {
                new ShaderFile(ShaderType.VertexShader, _path_glsl_effect + "gBuffer_Stencil.vert", stencil_helpers),
                new ShaderFile(ShaderType.FragmentShader, _path_glsl_effect + "gBuffer_Lighting_POINT.frag", point_lighting_helpers)
            });
            _pLighting_POINT.addUniform(RenderHelper.uModel);
            _pLighting_POINT.enable_LightCalculation();
            _pLighting_POINT.enable_Samplers(3);
            _pLighting_POINT.addUniform("shadow_id");

            // Accumulate Lighting
            _pAccumulation = _pLoader.createProgram_PostProcessing(new ShaderFile[]
            {
                new ShaderFile(ShaderType.FragmentShader, _path_glsl_effect + "gBuffer_Accumulation.frag", null)
            });
            _pAccumulation.enable_Samplers(5);
        }

        protected override void load_Buffers()
        {

            _tDepthStencil = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Depth32fStencil8, PixelFormat.DepthComponent, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tDepthStencil.load();

            _tDiffuse_ID = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tDiffuse_ID.load();

            _tNormal_Depth = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tNormal_Depth.load();

            _tSpecular = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Rgba16, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tSpecular.load();

            _tVelocity = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Rg16f, PixelFormat.Rg, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tVelocity.load();

            _tLighting_Diffuse = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tLighting_Diffuse.load();

            _tLighting_Specular = new Texture(TextureTarget.Texture2D,
                _resolution.W, _resolution.H,
                0, false, false,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Linear, TextureMagFilter.Linear, TextureWrapMode.Clamp);
            _tLighting_Specular.load();

            _fGBuffer = new FrameBuffer("gBuffer");
            _fGBuffer.load(new Dictionary<FramebufferAttachment, Texture>()
            {
                { FramebufferAttachment.DepthStencilAttachment, _tDepthStencil },
                { FramebufferAttachment.ColorAttachment0, _tDiffuse_ID },
                { FramebufferAttachment.ColorAttachment1, _tNormal_Depth },
                { FramebufferAttachment.ColorAttachment2, _tSpecular },
                { FramebufferAttachment.ColorAttachment3, _tVelocity },
                { FramebufferAttachment.ColorAttachment6, _tLighting_Diffuse },
                { FramebufferAttachment.ColorAttachment7, _tLighting_Specular }
            });
        }

        public override void load()
        {
            load_Programs();
            load_Buffers();
        }

        public override void unload()
        {

        }

        public override void reload()
        {

        }

        //------------------------------------------------------
        // Helpers
        //------------------------------------------------------
        public void toggleWireframe()
        {
            _enable_Wireframe = !_enable_Wireframe;
        }


        //------------------------------------------------------
        // Geometry
        //------------------------------------------------------

        private void pass_Geometry(Scene scene)
        {
            _fGBuffer.bind(new DrawBuffersEnum[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2,
                DrawBuffersEnum.ColorAttachment3
            });


            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, _resolution.W, _resolution.H);
            
            _pGeometry.bind();
            GL.Uniform1(_pGeometry.getUniform("enable_Wireframe"), _enable_Wireframe ? 1 : 0);
            GL.Uniform2(_pGeometry.getUniform("render_size"), _resolution.dimensions);

            scene.render(BeginMode.Patches, _pGeometry);
        }

        //------------------------------------------------------
        // Lighting
        //------------------------------------------------------

        private void pass_Stencil(Light l)
        {
            _fGBuffer.bindAttachements(DrawBuffersEnum.None);

            GL.DepthMask(false);
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            GL.Clear(ClearBufferMask.StencilBufferBit);

            GL.StencilFunc(StencilFunction.Always, 0, 0);
            GL.StencilOpSeparate(StencilFace.Back, StencilOp.Keep, StencilOp.IncrWrap, StencilOp.Keep);
            GL.StencilOpSeparate(StencilFace.Front, StencilOp.Keep, StencilOp.DecrWrap, StencilOp.Keep);

            _pStencil.bind();
            WorldDrawer.drawLightBounds(l, _pStencil);
        }

        private void pass_sLight(Light l, Texture shadowDepthTexture)
        {
            _fGBuffer.bindAttachements(new DrawBuffersEnum[] {
                DrawBuffersEnum.ColorAttachment6,
                DrawBuffersEnum.ColorAttachment7
            });
            GL.StencilFunc(StencilFunction.Notequal, 0, 0xFF);

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);

            _pLighting_SPOT.bind();

            // Bind gBuffer Textures
            _tNormal_Depth.bind(_pLighting_SPOT.getSamplerUniform(0), 0);
            _tSpecular.bind(_pLighting_SPOT.getSamplerUniform(1), 1);
            shadowDepthTexture.bind(_pLighting_SPOT.getSamplerUniform(2), 2);

            // Load light uniforms
            GL.Uniform3(_pLighting_SPOT.getUniform(RenderHelper.uLightPosition), l.spatial.position);
            GL.Uniform3(_pLighting_SPOT.getUniform(RenderHelper.uLightColor), l.color);
            GL.Uniform1(_pLighting_SPOT.getUniform(RenderHelper.uLightIntensity), l.intensity);
            GL.Uniform1(_pLighting_SPOT.getUniform(RenderHelper.uLightFalloff), l.falloff);
            // Spot light specific properties
            GL.Uniform3(_pLighting_SPOT.getUniform(RenderHelper.uLightDirection), l.spatial.look);
            GL.Uniform1(_pLighting_SPOT.getUniform(RenderHelper.uLightSpotAngle), l.spot_angle);
            GL.Uniform1(_pLighting_SPOT.getUniform(RenderHelper.uLightSpotBlur), l.spot_blur);

            GL.Uniform1(_pLighting_SPOT.getUniform("shadow_id"), l.sid);

            WorldDrawer.drawLightBounds(l, _pLighting_SPOT);

        }

        private void pass_pLight(Light l, Texture shadowDepthTexture)
        {
            _fGBuffer.bindAttachements(new DrawBuffersEnum[] {
                DrawBuffersEnum.ColorAttachment6,
                DrawBuffersEnum.ColorAttachment7
            });
            GL.StencilFunc(StencilFunction.Notequal, 0, 0xFF);

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);

            _pLighting_POINT.bind();

            // Bind gBuffer Textures
            _tNormal_Depth.bind(_pLighting_POINT.getSamplerUniform(0), 0);
            _tSpecular.bind(_pLighting_POINT.getSamplerUniform(1), 1);
            shadowDepthTexture.bind(_pLighting_POINT.getSamplerUniform(2), 2);

            // Load light uniforms
            GL.Uniform3(_pLighting_POINT.getUniform(RenderHelper.uLightPosition), l.spatial.position);
            GL.Uniform3(_pLighting_POINT.getUniform(RenderHelper.uLightColor), l.color);
            GL.Uniform1(_pLighting_POINT.getUniform(RenderHelper.uLightIntensity), l.intensity);
            GL.Uniform1(_pLighting_POINT.getUniform(RenderHelper.uLightFalloff), l.falloff);

            GL.Uniform1(_pLighting_POINT.getUniform("shadow_id"), l.sid);

            WorldDrawer.drawLightBounds(l, _pLighting_POINT);
        }


        //------------------------------------------------------
        // Passes
        //------------------------------------------------------

        public void pass_DeferredShading(Scene scene, fx_Shadow fx_Shadow)
        {
            //------------------------------------------------------
            // Clear Lighting Buffer from last frame
            //------------------------------------------------------
            _fGBuffer.bind(new DrawBuffersEnum[] {
                DrawBuffersEnum.ColorAttachment6,
                DrawBuffersEnum.ColorAttachment7
            });
            GL.Clear(ClearBufferMask.ColorBufferBit);

            //------------------------------------------------------
            // Fill gBuffer with Scene
            //------------------------------------------------------
            pass_Geometry(scene);

            //------------------------------------------------------
            // Accumulate Lighting from Scene
            //------------------------------------------------------
            GL.Enable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            foreach (Light l in scene.lights)
            {
                switch (l.type)
                {
                    case Light.type_spot:
                        pass_Stencil(l);
                        pass_sLight(l, fx_Shadow.tSpot);
                        break;
                    case Light.type_point:
                        pass_Stencil(l);
                        pass_pLight(l, fx_Shadow.tPoint);
                        break;
                }
            }


            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            //GL.Disable(EnableCap.DepthTest);
        }

        public void pass_LightAccumulation(fx_Quad quad, Texture atmoshpere_texture, Texture indirect_texture, FrameBuffer fFinalScene)
        {
            fFinalScene.bind(DrawBuffersEnum.ColorAttachment0);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Viewport(0, 0, _resolution.W, _resolution.H);

            _pAccumulation.bind();

            _tDiffuse_ID.bind(_pAccumulation.getSamplerUniform(0), 0);
            _tLighting_Diffuse.bind(_pAccumulation.getSamplerUniform(1), 1);
            _tLighting_Specular.bind(_pAccumulation.getSamplerUniform(2), 2);
            atmoshpere_texture.bind(_pAccumulation.getSamplerUniform(3), 3);
            indirect_texture.bind(_pAccumulation.getSamplerUniform(4), 4);

            quad.render();
        }
    }
}
