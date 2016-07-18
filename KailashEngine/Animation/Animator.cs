﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace KailashEngine.Animation
{
    class Animator
    {

        public struct KeyFrame
        {
            // The time this key frame triggers
            public float time;

            // Frame data
            public float data;

            // Bezier interpolation values
            public Vector4 bezier_values;


            public KeyFrame(float time, float data, Vector4 bezier_values)
            {
                this.time = time;

                this.data = data;
                this.bezier_values = bezier_values;
            }
        };



        private string _id;
        public string id
        {
            get { return _id; }
            set { _id = value; }
        }

        
        private Dictionary<float, KeyFrame> _key_frames_location_x;
        private Dictionary<float, KeyFrame> _key_frames_location_y;
        private Dictionary<float, KeyFrame> _key_frames_location_z;

        private Dictionary<float, KeyFrame> _key_frames_rotation_x;
        private Dictionary<float, KeyFrame> _key_frames_rotation_y;
        private Dictionary<float, KeyFrame> _key_frames_rotation_z;

        private Dictionary<float, KeyFrame> _key_frames_scale_x;
        private Dictionary<float, KeyFrame> _key_frames_scale_y;
        private Dictionary<float, KeyFrame> _key_frames_scale_z;


        public Animator(string id)
        {
            _id = id;
           
            _key_frames_location_x = new Dictionary<float, KeyFrame>();
            _key_frames_location_y = new Dictionary<float, KeyFrame>();
            _key_frames_location_z = new Dictionary<float, KeyFrame>();

            _key_frames_rotation_x = new Dictionary<float, KeyFrame>();
            _key_frames_rotation_y = new Dictionary<float, KeyFrame>();
            _key_frames_rotation_z = new Dictionary<float, KeyFrame>();

            _key_frames_scale_x = new Dictionary<float, KeyFrame>();
            _key_frames_scale_y = new Dictionary<float, KeyFrame>();
            _key_frames_scale_z = new Dictionary<float, KeyFrame>();
        }


        //------------------------------------------------------
        // Build Key Frame Dictionaries
        //------------------------------------------------------

        public void addKeyFrame(float time, string action, string channel, float data, Vector4 data_bezier)
        {
            // Decide which Key Frame dictionary to add action to
            Dictionary<float, KeyFrame> temp_dictionary = null;
            switch (action)
            {
                case AnimationHelper.translate:
                    switch (channel)
                    {
                        case "X":
                            temp_dictionary = _key_frames_location_x;
                            break;
                        case "Y":
                            temp_dictionary = _key_frames_location_y;
                            break;
                        case "Z":
                            temp_dictionary = _key_frames_location_z;
                            break;
                    }
                    break;
                case AnimationHelper.rotation_euler:
                    switch (channel)
                    {
                        case "X":
                            temp_dictionary = _key_frames_rotation_x;
                            break;
                        case "Y":
                            temp_dictionary = _key_frames_rotation_y;
                            break;
                        case "Z":
                            temp_dictionary = _key_frames_rotation_z;
                            break;
                    }
                    break;
                case AnimationHelper.scale:
                    switch (channel)
                    {
                        case "X":
                            temp_dictionary = _key_frames_scale_x;
                            break;
                        case "Y":
                            temp_dictionary = _key_frames_scale_y;
                            break;
                        case "Z":
                            temp_dictionary = _key_frames_scale_z;
                            break;
                    }
                    break;
            }

            // Finally, add key frame to dictionary
            KeyFrame temp_key_frame = new KeyFrame(time, data, data_bezier);
            temp_dictionary.Add(time, temp_key_frame);
        }


        //------------------------------------------------------
        // Get Animation Data
        //------------------------------------------------------

        private float bezierInterpolation(KeyFrame previous_frame, KeyFrame next_frame, float current_time)
        {
            BezierCurveCubic temp_bezier = new BezierCurveCubic();
            temp_bezier.StartAnchor = new Vector2(previous_frame.time, previous_frame.data);
            temp_bezier.FirstControlPoint = previous_frame.bezier_values.Zw;
            temp_bezier.SecondControlPoint = next_frame.bezier_values.Xy;
            temp_bezier.EndAnchor = new Vector2(next_frame.time, next_frame.data);

            return temp_bezier.CalculatePoint(current_time).Y;
        }


        private float getData(Dictionary<float, KeyFrame> key_frame_dictionary, float current_time, float last_animation_frame_time)
        {
            List<float> key_frame_times = key_frame_dictionary.Keys.ToList();
            //key_frame_times.Sort();

            float num_repeats = 1;

            float last_frame_time = key_frame_times.Last();
            float repeat_multiplier = (num_repeats == -1) ? (float)Math.Floor(current_time / last_frame_time) : Math.Min((float)Math.Floor(current_time / last_frame_time), num_repeats - 1);
            float repeat_frame = repeat_multiplier * last_frame_time;
            float loop_time = current_time - repeat_frame;

            // Get prevous and next frame with interpolation between them
            Vector3 PrevNextInterp = getNearestFrame(key_frame_times.ToArray(), loop_time);

            float output = 0;

            if (last_animation_frame_time > last_frame_time && current_time > last_frame_time)
            {
                output = key_frame_dictionary[last_frame_time].data;
            }
            else
            {
                output = key_frame_dictionary[PrevNextInterp.X].data;

                if (PrevNextInterp.Z != -1)
                {
                    KeyFrame previous_frame = key_frame_dictionary[PrevNextInterp.X];
                    KeyFrame next_frame = key_frame_dictionary[PrevNextInterp.Y];
                    float interpolation = PrevNextInterp.Z;

                    output = bezierInterpolation(previous_frame, next_frame, interpolation);
                }
            }

            return output;
        }

        public float getLastAnimationFrameTime(List<List<float>> key_frame_dictionaries)
        {
            float last_frame_time = 0;

            foreach (List<float> frames in key_frame_dictionaries)
            {
                last_frame_time = Math.Max(frames.Last(), last_frame_time);
            }

            return last_frame_time;
        }


        public Matrix4 getKeyFrame(float time, int num_repeats)
        {
            Matrix4 temp_matrix = Matrix4.Identity;

            //List<float> frame_times = _key_frames.Keys.ToList();
            //frame_times.Sort();

            // Last key frame time
            //float max_frame = frame_times.Last();
            //float repeat_multiplier = (num_repeats == -1) ? (float)Math.Floor(time / max_frame) : Math.Min((float)Math.Floor(time / max_frame), num_repeats - 1);
            //float repeat_frame = repeat_multiplier * max_frame;
            //float loop_time = time - repeat_frame;
            float loop_time = time;


            float last_animation_frame_time = getLastAnimationFrameTime(new List<List<float>>
            {
                _key_frames_location_x.Keys.ToList(),
                _key_frames_location_y.Keys.ToList(),
                _key_frames_location_z.Keys.ToList(),
                _key_frames_rotation_x.Keys.ToList(),
                _key_frames_rotation_y.Keys.ToList(),
                _key_frames_rotation_z.Keys.ToList(),
                _key_frames_scale_x.Keys.ToList(),
                _key_frames_scale_y.Keys.ToList(),
                _key_frames_scale_z.Keys.ToList(),
            });

            // Set animation actions
            Vector3 translation = new Vector3(
                getData(_key_frames_location_x, time, last_animation_frame_time),
                getData(_key_frames_location_y, time, last_animation_frame_time),
                getData(_key_frames_location_z, time, last_animation_frame_time)
            );

            Vector3 rotation_euler = new Vector3(
                getData(_key_frames_rotation_x, time, last_animation_frame_time),
                getData(_key_frames_rotation_y, time, last_animation_frame_time),
                getData(_key_frames_rotation_z, time, last_animation_frame_time)
            );

            Vector3 scale = new Vector3(
                getData(_key_frames_scale_x, time, last_animation_frame_time),
                getData(_key_frames_scale_y, time, last_animation_frame_time),
                getData(_key_frames_scale_z, time, last_animation_frame_time)
            );



            // Create Action Matrix
            Matrix4 yup = Matrix4.CreateRotationX((float)(-90.0f * Math.PI / 180.0f));


            // Scale
            Matrix4 temp_scale = Matrix4.CreateScale(scale);

            // Rotation
            float x_angle = rotation_euler.X;
            float y_angle = rotation_euler.Y;
            float z_angle = rotation_euler.Z;

            Quaternion x_rotation = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(x_angle));
            Quaternion y_rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(y_angle));
            Quaternion z_rotation = Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(z_angle));
            Quaternion zyx_rotation = Quaternion.Multiply(Quaternion.Multiply(z_rotation, y_rotation), x_rotation);

            zyx_rotation.Normalize();
            Matrix4 temp_rotation = Matrix4.CreateFromQuaternion(zyx_rotation);

            // Translation
            Matrix4 temp_translation = Matrix4.CreateTranslation(translation);


            // Build full tranformation matrix
            temp_matrix = temp_scale * (temp_rotation * temp_translation);

            // Blender defaults to Z-up. Need to convert to Y-up.
            temp_matrix = temp_matrix * yup;


            return temp_matrix;
        }


        // Get frame before and after the submitted time
        private Vector3 getNearestFrame(float[] frame_times, float time)
        {
            float previous_frame = frame_times[0];
            float next_frame;
            for(int i = 0; i < frame_times.Length; i++)
            {
                if(time == frame_times[i])
                {
                    return new Vector3(time, time, -1.0f);
                }
                else if (time > frame_times[i])
                {
                    previous_frame = frame_times[i];
                }
                else if (time < frame_times[i])
                {
                    next_frame = frame_times[i];

                    float interpolation = (time - previous_frame) / (next_frame - previous_frame);

                    return new Vector3(previous_frame, next_frame, interpolation);
                }
            }

            return new Vector3(previous_frame, previous_frame, 0);
        }

    }
}
