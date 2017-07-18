﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

using MuffinEngine.Animation;
using MuffinEngine.Physics;

namespace MuffinEngine.World.Model
{
    class UniqueMesh
    {

        private string _id;
        public string id
        {
            get { return _id; }
            set { _id = value; }
        }


        protected Matrix4 _base_transformation;
        public Matrix4 base_transformation
        {
            get { return _base_transformation; }
            set { _base_transformation = value; }
        }

        protected Matrix4 _transformation;
        public Matrix4 transformation
        {
            get { return _transformation; }
            set { _transformation = value; }
        }

        protected Matrix4 _previous_transformation;
        public Matrix4 previous_transformation
        {
            get { return _previous_transformation; }
            set { _previous_transformation = value; }
        }



        private Mesh _mesh;
        public Mesh mesh
        {
            get { return _mesh; }
            set { _mesh = value; }
        }



        private ObjectAnimator _animator;
        public ObjectAnimator animator
        {
            get { return _animator; }
            set
            {
                animated = true;
                _animator = value;
            }
        }

        private bool _animated;
        public bool animated
        {
            get { return _animated; }
            set { _animated = value; }
        }



        private PhysicsObject _physics_object;
        public PhysicsObject physics_object
        {
            get { return _physics_object; }
            set
            {
                _physical = true;
                _physics_object = value;
            }
        }

        private bool _physical;
        public bool physical
        {
            get { return _physical; }
            set { _physical = value; }
        }



        public UniqueMesh(string id, Mesh mesh, Matrix4 transformation)
        {
            _id = id;
            _mesh = mesh;
            _base_transformation = transformation;
            _transformation = _base_transformation;
            _previous_transformation = _base_transformation;
            _animated = false;
            _physical = false;
        }
    }
}
