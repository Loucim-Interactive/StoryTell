using System.Numerics;

namespace InmersionSystem
{
    public interface ICharacterLocomotion
    {
        bool IsGrounded { get; }
        bool IsMoving { get; }
        bool IsSprinting { get; }
        Vector3 Velocity { get; }
    }
}