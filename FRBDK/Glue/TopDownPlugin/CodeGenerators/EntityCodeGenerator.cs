﻿using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.CodeGenerators
{
    class EntityCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation => CodeLocation.AfterStandardGenerated;

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            /////////////////Early Out//////////////////////
            if(GetIfIsTopDown(element) == false)
            {
                return codeBlock;
            }
            //////////////End Early Out//////////////////////
            ///
            codeBlock.Line("#region Top Down Fields");


            codeBlock.Line("DataTypes.TopDownValues mCurrentMovement;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current movement variables used when applying input.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public DataTypes.TopDownValues", "CurrentMovement")
                .Get()
                    .Line("return mCurrentMovement;");

            codeBlock.AutoProperty("private FlatRedBall.Input.IInputDevice", "InputDevice");

            codeBlock.Line("TopDownDirection mDirectionFacing;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Which direciton the character is facing.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("protected TopDownDirection", "DirectionFacing")
                .Get()
                    .Line("return mDirectionFacing;");

            codeBlock.Property("public PossibleDirections", "PossibleDirections")
                .AutoGet().End()
                .AutoSet();

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls the horizontal movement of the character.");
            codeBlock.Line("/// Common examples include a d-pad, analog stick, or keyboard keys.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.I2DInput", "MovementInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether input is read to control the movement of the character.");
            codeBlock.Line("/// This can be turned off if the player should not be able to control");
            codeBlock.Line("/// the character.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public bool", "InputEnabled");

            codeBlock.Line("#endregion");

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            /////////////////Early Out//////////////////////
            if (GetIfIsTopDown(element) == false)
            {
                return codeBlock;
            }
            //////////////End Early Out//////////////////////

            // The platformer plugin sets events here, but we don't need to
            // here on the top-down (yet) since there are no events
            // Update 1 - actually we should prob assign the default movement
            // here if there is one...
            codeBlock.Line("InitializeInput();");

            var ifBlock = codeBlock.If("TopDownValues?.Count > 0");
            {
                ifBlock.Line("mCurrentMovement = TopDownValues.Values.FirstOrDefault();");
            }
            codeBlock.Line("PossibleDirections = PossibleDirections.FourWay;");

            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if (!GetIfIsTopDown(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line(
@"
        #region Top-Down Methods
        /// <summary>
        /// Sets the MovementInput to either the keyboard or 
        /// Xbox360GamePad index 0. This can be overridden by base classes to default
        /// to different input devices.
        /// </summary>
        protected virtual void InitializeInput()
        {
            if (FlatRedBall.Input.InputManager.Xbox360GamePads[0].IsConnected)
            {
                InitializeTopDownInput(FlatRedBall.Input.InputManager.Xbox360GamePads[0]);
            }
            else
            {
                InitializeTopDownInput(FlatRedBall.Input.InputManager.Keyboard);
            }
        
            InputEnabled = true;
        }
        

        public void InitializeTopDownInput(FlatRedBall.Input.IInputDevice inputDevice)
        {
            this.MovementInput = inputDevice.Default2DInput;
            this.InputDevice = inputDevice;
            InputEnabled = true;
        }


        private void ApplyMovementInput()
        {
            ////////early out/////////
            if(mCurrentMovement == null)
            {
                return;
            }
            //////end early out

            var velocity = this.Velocity;

            var desiredVelocity = Microsoft.Xna.Framework.Vector3.Zero;

            if(InputEnabled)
            {
                desiredVelocity = new Microsoft.Xna.Framework.Vector3(MovementInput.X, MovementInput.Y, velocity.Z) * 
                    mCurrentMovement.MaxSpeed;
            }

            var difference = desiredVelocity - velocity;

            Acceleration = Microsoft.Xna.Framework.Vector3.Zero;

            var differenceLength = difference.Length();

            const float differenceEpsilon = .1f;

            if (differenceLength > differenceEpsilon)
            {
                var isMoving = velocity.X != 0 || velocity.Y != 0;
                var isDesiredVelocityNonZero = desiredVelocity.X != 0 || desiredVelocity.Y != 0;

                // A 0 to 1 ratio of acceleration to deceleration, where 1 means the player is accelerating completely,
                // and 0 means decelerating completely. This value will often be between 0 and 1 because the player may
                // set desired velocity perpendicular to the current velocity
                float accelerationRatio = 1;
                if(isMoving && isDesiredVelocityNonZero == false)
                {
                    // slowing down completely
                    accelerationRatio = 0;
                }
                else if(isMoving == false && isDesiredVelocityNonZero)
                {
                    accelerationRatio = 1;
                }
                else
                {
                    // both is moving and has a non-zero desired value
                    var movementAngle = (float)Math.Atan2(velocity.Y, velocity.X);
                    var desiredAngle = (float)Math.Atan2(difference.Y, difference.X);

                    accelerationRatio = 1-
                        Math.Abs(FlatRedBall.Math.MathFunctions.AngleToAngle(movementAngle, desiredAngle)) / (float)Math.PI;

                }

                var secondsToTake = Microsoft.Xna.Framework.MathHelper.Lerp(
                    mCurrentMovement.DecelerationTime,
                    mCurrentMovement.AccelerationTime,
                    accelerationRatio);

                if(secondsToTake == 0)
                {
                    this.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                    this.Velocity = desiredVelocity;
                }
                else
                {
                    var accelerationMagnitude = mCurrentMovement.MaxSpeed / secondsToTake;
                
                    var nonNormalizedDifference = difference;
                
                    difference.Normalize();
                
                    var accelerationToSet = accelerationMagnitude * difference;
                    var expectedVelocityToAdd = accelerationToSet * TimeManager.SecondDifference;
                
                    if(expectedVelocityToAdd.Length() > nonNormalizedDifference.Length())
                    {
                        // we will overshoot it, so let's adjust the acceleration accordingly:
                        var ratioOfToAdd = nonNormalizedDifference.Length() / expectedVelocityToAdd.Length();
                        this.Acceleration = accelerationToSet * ratioOfToAdd;
                    }
                    else
                    {
                        this.Acceleration = accelerationToSet;
                    }
                }

                const float velocityEpsilon = .1f;
                var shouldAssignDirection = this.Velocity.Length() > velocityEpsilon || difference.Length() > 0;
                // player stopped moving, don't apply direction
                if(this.Velocity.LengthSquared() == 0)
                {
                    shouldAssignDirection = false;
                }

                if (shouldAssignDirection && mCurrentMovement.UpdateDirectionFromVelocity)
                {
                    mDirectionFacing = TopDownDirectionExtensions.FromDirection(XVelocity, YVelocity, PossibleDirections);
                }
            }
            else
            {
                Velocity = desiredVelocity;
                Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
            }

        }

        #endregion

");

            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if (!GetIfIsTopDown(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line("ApplyMovementInput();");

            return codeBlock;
        }

        private bool GetIfIsTopDown(IElement element)
        {
            return element.Properties
                .GetValue<bool>(nameof(TopDownEntityViewModel.IsTopDown));
        }
    }
}
