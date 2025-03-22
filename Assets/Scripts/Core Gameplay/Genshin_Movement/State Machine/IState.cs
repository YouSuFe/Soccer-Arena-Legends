using UnityEngine;

public interface IState
{
    public void Enter();
    public void HandleInput();
    public void Update();
    public void CameraUpdate();
    public void PhysicsUpdate();
    public void Exit();
    public void OnAnimationEnterEvent();
    public void OnAnimationExitEvent();
    public void OnAnimationTransitionEvent();
    public void OnTriggerEnter(Collider collider);
    public void OnTriggerExit(Collider collider);
    public void OnCollisionEnter(Collision collision);
}
