using Newtonsoft.Json;
using UnityEngine;

namespace RTSLockstep
{
    public class Turn : Ability
    {
        #region Serialized
        [SerializeField, VectorRotation(true)]
        private Vector2d _turnRate = Vector2d.CreateRotation(FixedMath.One / 8);
        #endregion
        private bool targetReached;
        private Vector2d targetRotation;
        private long turnSin;
        private long turnCos;
        private long cachedBeginCheck;
        const int checkCollisionTurnRate = 1;//LockstepManager.FrameRate;
                                             //private int checkCollisionCount;

        private long collisionTurnThreshold;
        bool bufferStartTurn;
        Vector2d bufferTargetRotation;
        bool isColliding;

        protected override void OnSetup()
        {
            Agent.Body = Agent.Body;

            turnSin = _turnRate.y;
            turnCos = _turnRate.x;

            collisionTurnThreshold = Agent.Body.Radius / (LockstepManager.FrameRate / 2);
            collisionTurnThreshold *= collisionTurnThreshold;
            Agent.Body.onContact += HandleContact;
        }

        protected override void OnInitialize()
        {
            targetReached = true;
            targetRotation = Vector2d.up;
            //checkCollisionCount = 0;
            cachedBeginCheck = 0;

            bufferStartTurn = false;
        }

        void CheckAutoturn()
        {
            if (isColliding)
            {
                isColliding = false;
                //autoturn direction will be culmination of positional changes
                if (targetReached == true && Agent.IsCasting == false && !(Agent.Body.Immovable || Agent.Body.IsTrigger))
                {
                    Vector2d delta = this.Agent.Body.Position - this.Agent.Body.LastPosition;
                    if (delta.FastMagnitude() > collisionTurnThreshold)
                    {
                        delta.Normalize();
                        this.StartTurnDirection(delta);
                    }
                }
            }
        }

        protected override void OnSimulate()
        {
            if (targetReached == false)
            {
                if (cachedBeginCheck != 0)
                {
                    {
                        if (cachedBeginCheck < 0)
                        {
                            Agent.Body.Rotate(turnCos, turnSin);
                        }
                        else
                        {
                            Agent.Body.Rotate(turnCos, -turnSin);
                        }
                    }

                }
                else
                {
                    if (Agent.Body._rotation.Dot(targetRotation.x, targetRotation.y) < 0)
                    {
                        Agent.Body.Rotate(turnCos, turnSin);
                    }
                    else
                    {
                        Arrive();
                    }
                }
                Agent.Body.RotationChanged = true;
            }
        }

        protected override void OnLateSimulate()
        {
            if (targetReached == false)
            {
                long check = Agent.Body._rotation.Cross(targetRotation.x, targetRotation.y);
                if (check == 0 || ((cachedBeginCheck < 0) != (check < 0)))
                {
                    Arrive();
                }
            }
            else
            {
                CheckAutoturn();

            }
            if (bufferStartTurn)
            {
                bufferStartTurn = false;
                _StartTurn(bufferTargetRotation);
            }
        }

        private void Arrive()
        {
            Agent.Body._rotation = targetRotation;
            Agent.Body.RotationChanged = true;
            targetReached = true;
        }

        public void StartTurnVector(Vector2d targetVector)
        {
            targetVector.Normalize();
            StartTurnDirection(targetVector);
        }

        public void StartTurnDirection(Vector2d targetDirection)
        {
            bufferStartTurn = true;
            bufferTargetRotation = targetDirection.ToRotation();
        }

        //TODO: Implement this!
        public void SetDefaultTurn(Vector2d targetDirection)
        {

        }

        private void _StartTurn(Vector2d targetRot)
        {
            long tempCheck;
            if (targetRot.NotZero() &&
               (((tempCheck = Agent.Body._rotation.Cross(targetRot.x, targetRot.y)) != 0) ||
                (Agent.Body._rotation.Dot(targetRot.x, targetRot.y) < 0))
               )
            {
                if (tempCheck.AbsLessThan(turnSin) && Agent.Body._rotation.Dot(targetRot.x, targetRot.y) > 0)
                {
                    targetRotation = targetRot;
                    Arrive();
                }
                else
                {
                    cachedBeginCheck = tempCheck;
                    targetRotation = targetRot;
                    targetReached = false;
                }
            }
            else
            {

            }
        }

        public void StopTurn()
        {
            targetReached = true;
        }

        protected override void OnStopCast()
        {
            StopTurn();
        }

        private void HandleContact(LSBody other)
        {
            isColliding = true;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "TargetReached", targetReached);
            SaveManager.WriteVector2d(writer, "TargetRotation", targetRotation);
            SaveManager.WriteLong(writer, "CachedBeginCheck", cachedBeginCheck);
            SaveManager.WriteBoolean(writer, "BufferStartTurn", bufferStartTurn);
            SaveManager.WriteVector2d(writer, "bufferTargetRotation", bufferTargetRotation);
            SaveManager.WriteBoolean(writer, "Colliding", isColliding);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "TargetReached":
                    targetReached = (bool)readValue;
                    break;
                case "TargetRotation":
                    targetRotation = LoadManager.LoadVector2d(reader);
                    break;
                case "CachedBeginCheck":
                    cachedBeginCheck = (long)readValue;
                    break;
                case "BufferStartTurn":
                    bufferStartTurn = (bool)readValue;
                    break;
                case "bufferTargetRotation":
                    bufferTargetRotation = LoadManager.LoadVector2d(reader);
                    break;
                case "Colliding":
                    isColliding = (bool)readValue;
                    break;
                default:
                    break;
            }
        }
    }
}