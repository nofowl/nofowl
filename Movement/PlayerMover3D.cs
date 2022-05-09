using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoFowl.Movement
{
    public class PlayerMover3D : MonoBehaviour
    {
        [Header("Variables")]
        [SerializeField] float speed = 100.0f;
        [SerializeField] float rotationSpeed = 5.0f;

        [Header("Controllers")]
        [SerializeField] CharacterController controller;
        [SerializeField] Animator animator;
        [SerializeField] Transform rotatingTransform;

        // private
        Vector3 movement = Vector3.zero;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            // Get input
            GetMove();
        }

        private void FixedUpdate()
        {
            // Apply movement (prevent FR based physics).
            Move();
        }

        private void Move()
        {
            controller.SimpleMove(movement * speed * Time.fixedDeltaTime);
            animator.SetFloat("Move", movement.magnitude);

            if (movement.magnitude > 0.0f)
            {
                rotatingTransform.forward = Vector3.Lerp(rotatingTransform.forward, movement, Time.fixedDeltaTime * rotationSpeed); 
            }
        }

        private void GetMove()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            // clamp magnitude 
            movement = Vector3.ClampMagnitude(new Vector3(x, 0, y), 1.0f);
        }
    }
}
