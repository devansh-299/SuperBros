using UnityEngine;
using System.Collections;

// currently this script is also used for Fire gameobject
public class SpikeGround : MonoBehaviour {
	
	// damage given to the player
	public int damageAmount = 10;
		
	// for attacking the player
	void OnTriggerEnter2D(Collider2D collision) {

		if ((collision.tag == "Player")) {

			CharacterController2D playerScript = collision
				.gameObject.GetComponent<CharacterController2D>();
			if (playerScript.playerCanMove) {

				// apply damage to the player
				playerScript.ApplyDamage (damageAmount);

			}
		}
	}
}
