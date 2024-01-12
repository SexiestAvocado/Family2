using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

public struct PlayerCharacterInputs
{
  public float MoveAxisForward;
  public float MoveAxisRight;
  public Quaternion CameraRotation;
  public bool JumpDown;
  public bool CrouchDown;
  public bool CrouchUp;
}

public class MyCharacterController : MonoBehaviour, ICharacterController
{
  public KinematicCharacterMotor Motor;

  [Header("Stable Movement")]
  public float MaxStableMoveSpeed = 10f;
  public float StableMovementSharpness = 15;
  public float OrientationSharpness = 10;

  [Header("Air Movement")]
  public float MaxAirMoveSpeed = 10f;
  public float AirAccelerationSpeed = 5f;
  public float Drag = 0.1f;

  [Header("Jumping")]
  private bool AllowJumpingWhenSliding = true;
  private bool AllowDoubleJump = true;
  private bool AllowWallJump = true;
  public float JumpSpeed = 10f;
  public float JumpPreGroundingGraceTime = 0f;
  public float JumpPostGroundingGraceTime = 0f;

  [Header("Misc")]
  public Vector3 Gravity = new Vector3(0, -30f, 0);
  public bool OrientTowardsGravity = true;
  public Transform MeshRoot;

  //do not remove the "_" in _moveInputVector
  private Collider[] probedColliders = new Collider[8];
  private Vector3 _moveInputVector;
  private Vector3 lookInputVector;
  private bool jumpRequested = false;
  private bool jumpConsumed = false;
  private bool jumpedThisFrame = false;
  private float timeSinceJumpRequested = Mathf.Infinity;
  private float timeSinceLastAbleToJump = 0f;
  private bool doubleJumpConsumed = false;
  private bool canWallJump = false;
  private Vector3 wallJumpNormal;
  private Vector3 internalVelocityAdd = Vector3.zero;
  private bool shouldBeCrouching = false;
  private bool isCrouching = false;

  private void Start()
  {
    // Assign to motor
    Motor.CharacterController = this;
  }

  /// <summary>
  /// This is called every frame by MyPlayer in order to tell the character what its inputs are
   /// </summary>
  public void SetInputs(ref PlayerCharacterInputs inputs)
  {
    // Clamp input
    Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

    // Calculate camera direction and rotation on the character plane
    Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
    if (cameraPlanarDirection.sqrMagnitude == 0f)
    {
      cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
    }
    Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

    // Move and look inputs
    _moveInputVector = cameraPlanarRotation * moveInputVector;
    lookInputVector = cameraPlanarDirection;

    // Jumping input
    if (inputs.JumpDown)
    {
      timeSinceJumpRequested = 0f;
      jumpRequested = true;
    }
    // Crouching input
    if (inputs.CrouchDown)
    {
      shouldBeCrouching = true;

      if (!isCrouching)
      {
        isCrouching = true;
        Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
        MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
      }
    }
    else if (inputs.CrouchUp)
    {
      shouldBeCrouching = false;
    }
  }

  /// <summary>
  /// (Called by KinematicCharacterMotor during its update cycle)
  /// This is called before the character begins its movement update
  /// </summary>
  public void BeforeCharacterUpdate(float deltaTime)
  {
  }

  /// <summary>
  /// (Called by KinematicCharacterMotor during its update cycle)
  /// This is where you tell your character what its rotation should be right now. 
  /// This is the ONLY place where you should set the character's rotation
  /// </summary>
  public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
  {
    if (lookInputVector != Vector3.zero && OrientationSharpness > 0f)
    {
      // Smoothly interpolate from current to target look direction
      Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

      // Set the current rotation (which will be used by the KinematicCharacterMotor)
      currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
    }
    /*Orienting towards arbitrary up direction
    In order to demonstrate how you could orient the character towards any direction, we will now implement an option that allows the
    character to always orient its up direction in the opposite direction of the gravity*/
    if (OrientTowardsGravity)
    {
      // Rotate from current up to invert gravity
      currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
    }
  }

  /// <summary>
  /// (Called by KinematicCharacterMotor during its update cycle)
  /// This is where you tell your character what its velocity should be right now. 
  /// This is the ONLY place where you can set the character's velocity
  /// </summary>
  public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
  {
    Vector3 targetMovementVelocity = Vector3.zero;
    if (Motor.GroundingStatus.IsStableOnGround)
    {
      // Reorient velocity on slope
      currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

      // Calculate target velocity
      Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
      Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
      targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

      // Smooth movement Velocity
      currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
    }
    else
    {
      // Add move input
      if (_moveInputVector.sqrMagnitude > 0f)
      {
        targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

        // Prevent climbing on un-stable slopes with air movement
        if (Motor.GroundingStatus.FoundAnyGround)
        {
          Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
          targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
        }

        Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
        currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
      }

      // Gravity
      currentVelocity += Gravity * deltaTime;

      // Drag
      currentVelocity *= (1f / (1f + (Drag * deltaTime)));
    }

    // Handle jumping
    jumpedThisFrame = false;
    timeSinceJumpRequested += deltaTime;
    if (jumpRequested)
    {
      // Handle double jump
      if (AllowDoubleJump)
      {
        if (jumpConsumed && !doubleJumpConsumed && (AllowJumpingWhenSliding ? !Motor.GroundingStatus.FoundAnyGround : !Motor.GroundingStatus.IsStableOnGround))
        {
          Motor.ForceUnground(0.1f);

          // Add to the return velocity and reset jump state
          currentVelocity += (Motor.CharacterUp * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
          jumpRequested = false;
          doubleJumpConsumed = true;
          jumpedThisFrame = true;
        }
      }

      // See if we actually are allowed to jump
      if (canWallJump || (!jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
      {
        // Calculate jump direction before ungrounding
        Vector3 jumpDirection = Motor.CharacterUp;
        if (canWallJump)
        {
          jumpDirection = wallJumpNormal;
        }
        else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
        {
          jumpDirection = Motor.GroundingStatus.GroundNormal;
        }

        // Makes the character skip ground probing/snapping on its next update. 
        // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
        Motor.ForceUnground(0.1f);

        // Add to the return velocity and reset jump state
        currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
        jumpRequested = false;
        jumpConsumed = true;
        jumpedThisFrame = true;
      }
    }
    // Reset wall jump
    canWallJump = false;

    // Take into account additive impulse/velocity
    if (internalVelocityAdd.sqrMagnitude > 0f)
    {
      currentVelocity += internalVelocityAdd;
      internalVelocityAdd = Vector3.zero;
    }
  }

  /// <summary>
  /// (Called by KinematicCharacterMotor during its update cycle)
  /// This is called after the character has finished its movement update
  /// </summary>
  public void AfterCharacterUpdate(float deltaTime)
  {
    // Handle jump-related values
    {
      // Handle jumping pre-ground grace period
      if (jumpRequested && timeSinceJumpRequested > JumpPreGroundingGraceTime)
      {
        jumpRequested = false;
      }
      // Handle jumping while sliding
      if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
      {
        // If we're on a ground surface, reset jumping values
        if (!jumpedThisFrame)
        {
          doubleJumpConsumed = false;
          jumpConsumed = false;
        }
        timeSinceLastAbleToJump = 0f;
      }
      else
      {
        // Keep track of time since we were last able to jump (for grace period)
        timeSinceLastAbleToJump += deltaTime;
      }
    }
    // Handle uncrouching
    if (isCrouching && !shouldBeCrouching)
    {
      // Do an overlap test with the character's standing height to see if there are any obstructions
      Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
      if (Motor.CharacterCollisionsOverlap(
              Motor.TransientPosition,
              Motor.TransientRotation,
              probedColliders) > 0)
      {
        // If obstructions, just stick to crouching dimensions
        Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
      }
      else
      {
        // If no obstructions, uncrouch
        MeshRoot.localScale = new Vector3(1f, 1f, 1f);
        isCrouching = false;
      }
    }
  }

  public bool IsColliderValidForCollisions(Collider coll)
  {
    return true;
  }

  public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
  {
  }
  public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
  {
  // We can wall jump only if we are not stable on ground and are moving  against an obstruction
    if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
    {
      canWallJump = true;
      wallJumpNormal = hitNormal;
    }
  }

  public void PostGroundingUpdate(float deltaTime)
  {
    // Handle landing and leaving ground
    if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
    {
      OnLanded();
    }
    else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
    {
      OnLeaveStableGround();
    }
  }
  protected void OnLanded()
  {
    Debug.Log("Landed");
  }

  protected void OnLeaveStableGround()
  {
    Debug.Log("Left ground");
  }

  /*adds impulse/velocity
  It is often desirable to have a quick and easy way to add forces and impulses to the character, whether it’s for explosion forces, hit
  impacts, wind zones, etc…. In order to accomplish this, we will create an “AddVelocity” method in MyCharacterController, which will
  maintain an internal velocity vector to add to the final velocity in UpdateVelocity*/
  public void AddVelocity(Vector3 velocity)
  {
    internalVelocityAdd += velocity;
  }

  public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
  {
  }

  public void OnDiscreteCollisionDetected(Collider hitCollider)
  {
  }
 }