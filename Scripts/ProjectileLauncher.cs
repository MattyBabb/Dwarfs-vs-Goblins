using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    public GameObject projectile;

    static float SIN_45 = 0.70710678118f;

    float GRAVITY;

    public Vector2 FireProjectile(int type, Vector2 previousPos, GameObject thisObject, MovingObject target, float thisGravity)
    {
        GRAVITY = thisGravity;
        return CalculateGroundTrajectory(target, previousPos, thisObject);

    }

    Vector2 CalculateGroundTrajectory(MovingObject target, Vector2 previousPos, GameObject thisObject)
    {
        float time = 0;
        float velocity1 = 0, velocity2 = 0;

        //firstly get the distance between the enemy and the projectile
        float currentDistToEnemy = Vector2.Distance(thisObject.transform.position, target.transform.position);

        //get the direction from the cannon to the target and the direction the target is heading
        Vector2 cannonToTargetDirection = target.transform.position - thisObject.transform.position;
        cannonToTargetDirection = cannonToTargetDirection / cannonToTargetDirection.magnitude; //normalised
        Vector2 targetDirection = (Vector2)target.transform.position - previousPos;
        targetDirection = targetDirection / targetDirection.magnitude; //normalised

        //now get the angle between the two
        float angle = Vector2.Angle(cannonToTargetDirection, targetDirection);

        //now the target movementspeed, relative to the cannon position is  negative cos(angle) roughly and multiplied by the target speed
        float cosAngle = Mathf.Cos(angle * Mathf.PI / 180);
        //float cosAngleDegrees = cosAngle * Mathf.Rad2Deg;
        float targetRelativeSpeed = cosAngle * target.moveSpeed * -1;


        //we can use this movement speed to calculate the velocity we need to launch our projectile
        //this is done by using the quadratic method

        //b^2 - 4 ac
        velocity1 = Mathf.Pow(targetRelativeSpeed, 2) - (4 * currentDistToEnemy * GRAVITY / 2);

        //square root
        velocity1 = Mathf.Sqrt(Mathf.Abs(velocity1));

        //plus or minus
        velocity1 = -targetRelativeSpeed + velocity1;
        velocity2 = -targetRelativeSpeed - velocity1;

        // divided by 2a
        velocity1 = velocity1 / 2;
        velocity2 = velocity2 / 2;

        if (velocity2 > 0)
            time = (velocity2 * 2) / GRAVITY;
        else
            time = (velocity1 * 2) / GRAVITY;

        //now that we have the time and the velocity of the projectile, we can use them to get the end point
        Vector2 cannonFireDistance;
        if (velocity2 > 0)
            cannonFireDistance = cannonToTargetDirection * velocity2 * time;
        else
            cannonFireDistance = cannonToTargetDirection * velocity1 * time;

        Vector2 targetMovementVector = targetDirection * time * target.moveSpeed;

        Vector2 finalDirection = cannonFireDistance + targetMovementVector;

        finalDirection.Normalize();
        if (velocity1 > 0)
            finalDirection = finalDirection * velocity1;
        else
            finalDirection = finalDirection * velocity2;

        return finalDirection;
    }

}
