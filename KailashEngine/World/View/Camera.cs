﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

using MuffinEngine.World.Role;

namespace MuffinEngine.World.View
{
    class Camera : ControllableWorldObject
    {

        private bool _following;
        public bool following
        {
            get { return _following; }
            set { _following = value; }
        }


        private float _default_fov;
        private float _fov_current;
        private float _fov_previous;

        private float _default_aspect_ratio;
        private Vector2 _default_near_far;

        private Vector3 _position_current;
        private Vector3 _position_previous;


        private Matrix4 _previous_view_matrix;
        public Matrix4 previous_view_matrix
        {
            get { return _previous_view_matrix; }
            set { _previous_view_matrix = value; }
        }

        private Matrix4 _previous_perspective_matrix;
        public Matrix4 previous_perspective_matrix
        {
            get { return _previous_perspective_matrix; }
            set { _previous_perspective_matrix = value; }
        }

        public Matrix4 previous_view_perspective
        {
            get
            {
                return _previous_view_matrix * _previous_perspective_matrix;
            }
        }


        public Camera(string id, float fov, float aspect_ratio, Vector2 near_far)
            : this(id, new SpatialData(), fov, aspect_ratio, near_far)
        { }

        public Camera(string id, SpatialData spatial_data, float fov, float aspect_ratio, Vector2 near_far)
            : base (id, spatial_data)
        {
            _following = false;

            _default_fov = _fov_current = _fov_previous = fov;
            _default_aspect_ratio = aspect_ratio;
            _default_near_far = near_far;

            _position_current = _spatial.position;

            _spatial.setPerspective(_fov_current, _default_aspect_ratio, _default_near_far);
            _previous_view_matrix = _spatial.model_view;
            _previous_perspective_matrix = _spatial.perspective;
        }



        public void followCharacter(ControllableWorldObject character)
        {
            try
            {
                _spatial = character.spatial;
                _position_current = _spatial.position;
                _following = true;
            }
            catch(Exception e)
            {
                Debug.DebugHelper.logError("Error Following Character (" + character.id + ")", e.Message);
            }       
        }

        public void unfollowCharacter()
        {
            if(_following)
            {
                _spatial = new SpatialData(_spatial.position, _spatial.look, _spatial.up);
                _following = false;
            }
        }


        public void resetSmoothMovement()
        {
            _position_current = _spatial.position;
            _position_previous = Vector3.Zero;
        }

        public void smoothMovement(float smooth_movement_delay)
        {
            _position_current = _spatial.position - _position_current;
            _position_current = Vector3.Lerp(_position_previous, _position_current, smooth_movement_delay);
            _position_previous = _position_current;
            _spatial.position += _position_current;
            _position_current = _spatial.position;
        }



        public void zoom(bool zoom_in, float current_fps)
        {
            float zoom_speed = 500.0f;
            float max_zoom = 0.35f * _default_fov;
            float zoom_delay = 0.6f;

            if (zoom_in)
            {
                _fov_current -= zoom_speed / current_fps;
                _fov_current = Math.Max(_fov_current, max_zoom);
            }
            else
            {
                _fov_current += zoom_speed / current_fps;
                _fov_current = Math.Min(_fov_current, _default_fov);
            }

            _fov_current = EngineHelper.slerp(_fov_previous, _fov_current, zoom_delay);
            _fov_previous = _fov_current;

            _spatial.setPerspective(_fov_current, _default_aspect_ratio, _default_near_far);
        }




    }
}
