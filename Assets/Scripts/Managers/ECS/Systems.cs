using System;
using UnityEngine;
namespace ECS
{
    class MovementSystem : System
    {
        public MovementSystem(IWorld world) : base(world) { }

        public override void Update()
        {
            var components = ComponentManager.GetComponents(MovementComponent.s_code);
            foreach (var item in components)
            {
                var component = (MovementComponent)item;
                if (component.axisMoveName != null)
                    component.inputMoveValue = Input.GetAxis(component.axisMoveName);

                if (component.axisTurnName != null)
                    component.inputTurnValue = Input.GetAxis(component.axisTurnName);
            }

        }
        public override void FixedUpdate()
        {
            var components = ComponentManager.GetComponentCollection(MovementComponent.s_code);
            foreach (var item in components)
            {
                var component = (MovementComponent)item.Value;
                var movement = component.forward * component.velocity * Time.deltaTime * component.inputMoveValue;
                float turn = component.inputTurnValue * component.turnSpeed * Time.deltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

                var entity = (EntityCode)item.Key;

                var actor = ComponentManager.TryGetEntityComponent<ActorComponent>(entity, ActorComponent.s_code);
                if (actor == null)
                {
                    component.position += movement;
                    component.rotation *= turnRotation;
                }
                else
                {
                    var rigidbody = actor.instance.GetComponent<Rigidbody>();
                    rigidbody.MovePosition(rigidbody.position + movement);
                    component.position = rigidbody.position;


                    // Apply this rotation to the rigidbody's rotation.
                    rigidbody.MoveRotation(rigidbody.rotation * turnRotation);
                    component.rotation = rigidbody.rotation;
                }
            }
        }
    }
}