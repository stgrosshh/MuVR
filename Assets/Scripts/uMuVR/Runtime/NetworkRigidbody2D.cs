using System;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using TriInspector;
using UnityEngine;
using NetworkBehaviour = uMuVR.Enhanced.NetworkBehaviour;

namespace uMuVR {
	
	/// <summary>
	/// Component which synchronizes rigidbody properties across the network
	/// </summary>
	/// <remarks>NOTE: Component ported from Mirror</remarks>
	/// <remarks>Requires a <see cref="NetworkTransform"/> to synchronize position/rotation information!</remarks>
	[RequireComponent(typeof(NetworkTransform))]
	public class NetworkRigidbody2D : NetworkBehaviour {
		/// <summary>
		/// Rigidbody that we are managing (this allows managing a rigidbody on a different object)
		/// </summary>
		/// <remarks>Component automatically searches for a rigidbody on the same object and assigns it if it exists</remarks>
		[Title("Settings")]
		[Required]
		public Rigidbody2D target;

		/// <summary>
		/// Bool indicating if the rigidbody should be kinematic or not (use this instead of the same field on the rigidbody)
		/// </summary>
		[PropertyTooltip("Flag indicating weather or not the managed Rigidbody2D should be Kinematic")]
		public bool targetIsKinematic;

		/// <summary>
		/// Should clients run the simulations and propagate their results or should simulations be performed by the server and distributed?
		/// </summary>
		/// <remarks>If you wish to use sever simulations, it would probably be better to use FishNet's predictive physics stack</remarks>
		[PropertyTooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
		public bool clientAuthority = true;

		/// <summary>
		/// Weather or not we should sync velocity
		/// </summary>
		[field: Title("Velocity")]
		[field: PropertyTooltip("Syncs Velocity every SyncInterval")]
		[field: SerializeField] private bool syncVelocity = true;
		/// <summary>
		/// Weather or not we should reset velocity to zero every frame
		/// </summary>
		[field: PropertyTooltip("Set velocity to 0 each frame (only works if syncVelocity is false")]
		[field: HideIf(nameof(syncVelocity))]
		[field: SerializeField] private bool clearVelocity;

		/// <summary>
		/// Weather or not we should sync angular velocity
		/// </summary>
		[field: Title("Angular Velocity")]
		[field: PropertyTooltip("Syncs AngularVelocity every SyncInterval")]
		[field: SerializeField] private bool syncAngularVelocity = true;
		/// <summary>
		/// Weather or not we should reset angular velocity to zero every frame
		/// </summary>
		[field: PropertyTooltip("Set angularVelocity to 0 each frame (only works if syncAngularVelocity is false")]
		[field: HideIf(nameof(syncAngularVelocity))]
		[field: SerializeField] private bool clearAngularVelocity;

		/// <summary>
		///     Values sent on client with authority after they are sent to the server
		/// </summary>
		private ClientSyncState previousValue;

		/// <summary>
		/// Whenever the Unity UI updates, if we don't have an associated rigidbody assign the one on this object (if it exists)
		/// </summary>
		private new void OnValidate() {
			base.OnValidate();
			target ??= GetComponent<Rigidbody2D>();
			if (target is not null) targetIsKinematic = target.bodyType == RigidbodyType2D.Kinematic;
		}

		/// <summary>
		/// Bool indicating if we are in client authoritative mode and have authority
		/// </summary>
		private bool ClientWithAuthority => clientAuthority && IsOwner;
		/// <summary>
		/// Bool indicating if we are in server authoritative mode and have authority
		/// </summary>
		private bool ServerWithAuthority => IsServerInitialized && !clientAuthority;
		/// <summary>
		/// Bool indicating if we have authority to perform physics simulations
		/// </summary>
		private bool IsAuthority => ClientWithAuthority || ServerWithAuthority;

		#region Sync vars

		#region veclocity sync

		[ReadOnly, SerializeField] private Vector2 _velocity;
		public Vector2 velocity {
			get => _velocity;
			set {
				OnVelocityChanged(_velocity, value, false);
				target.linearVelocity = _velocity = value;
				if (IsAuthority && IsServerInitialized)
					ObserversSetVelocity(value);
				else if (ClientWithAuthority)
					ServerSetVelocity(value);
			}
		}

		[ServerRpc]
		private void ServerSetVelocity(Vector2 value) {
			OnVelocityChanged(_velocity, value, false);
			ObserversSetVelocity(value);
			_velocity = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetVelocity(Vector2 value) {
			OnVelocityChanged(_velocity, value, false);
			_velocity = value;
		}

		private void OnVelocityChanged(Vector2 _, Vector2 newValue, bool onServer) {
			if (IsAuthority) return;
			target.linearVelocity = newValue;
		}

		#endregion

		#region angular velocity sync

		[ReadOnly, SerializeField] private float _angularVelocity;
		public float angularVelocity {
			get => _angularVelocity;
			set {
				OnAngularVelocityChanged(_angularVelocity, value, false);
				target.angularVelocity = _angularVelocity = value;
				if (IsAuthority && IsServerInitialized)
					ObserversSetAngularVelocity(value);
				else if (ClientWithAuthority)
					ServerSetAngularVelocity(value);
			}
		}

		[ServerRpc]
		private void ServerSetAngularVelocity(float value) {
			OnAngularVelocityChanged(_angularVelocity, value, false);
			ObserversSetAngularVelocity(value);
			_angularVelocity = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetAngularVelocity(float value) {
			OnAngularVelocityChanged(_angularVelocity, value, false);
			_angularVelocity = value;
		}

		private void OnAngularVelocityChanged(float _, float newValue, bool onServer) {
			if (IsAuthority) return;
			target.angularVelocity = newValue;
		}

		#endregion

		#region is kinematic sync

		[ReadOnly, SerializeField] private bool _isKinematic;
		public bool isKinematic {
			get => _isKinematic;
			set {
				OnIsKinematicChanged(_isKinematic, value, false);
				targetIsKinematic = _isKinematic = value;
				UpdateOwnershipKinematicState();
				if (IsAuthority && IsServerInitialized)
					ObserversSetIsKinematic(value);
				else if (ClientWithAuthority)
					ServerSetIsKinematic(value);
			}
		}

		[ServerRpc]
		private void ServerSetIsKinematic(bool value) {
			OnIsKinematicChanged(_isKinematic, value, false);
			ObserversSetIsKinematic(value);
			_isKinematic = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetIsKinematic(bool value) {
			OnIsKinematicChanged(_isKinematic, value, false);
			_isKinematic = value;
		}

		private void OnIsKinematicChanged(bool _, bool newValue, bool onServer) {
			if (IsAuthority) return;
			targetIsKinematic = newValue;
			UpdateOwnershipKinematicState();
		}

		#endregion

		#region use gravity sync

		[ReadOnly, SerializeField] private float _gravityScale;
		public float gravityScale {
			get => _gravityScale;
			set {
				OnGravityScaleChanged(_gravityScale, value, false);
				target.gravityScale = _gravityScale = value;
				if (IsAuthority && IsServerInitialized)
					ObserversSetGravityScale(value);
				else if (ClientWithAuthority)
					ServerSetGravityScale(value);
			}
		}

		[ServerRpc]
		private void ServerSetGravityScale(float value) {
			OnGravityScaleChanged(_gravityScale, value, false);
			ObserversSetGravityScale(value);
			_gravityScale = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetGravityScale(float value) {
			OnGravityScaleChanged(_gravityScale, value, false);
			_gravityScale = value;
		}

		private void OnGravityScaleChanged(float _, float newValue, bool onServer) {
			if (IsAuthority) return;
			target.gravityScale = newValue;
		}

		#endregion

		#region drag sync

		[ReadOnly, SerializeField] private float _drag;
		public float drag {
			get => _drag;
			set {
				OnDragChanged(_drag, value, false);
				target.linearDamping = _drag = value;
				if (IsAuthority && IsServerInitialized)
					ObserversSetDrag(value);
				else if (ClientWithAuthority)
					ServerSetDrag(value);
			}
		}

		[ServerRpc]
		private void ServerSetDrag(float value) {
			OnDragChanged(_drag, value, false);
			ObserversSetDrag(value);
			_drag = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetDrag(float value) {
			OnDragChanged(_drag, value, false);
			_drag = value;
		}

		private void OnDragChanged(float _, float newValue, bool onServer) {
			if (IsAuthority) return;
			target.linearDamping = newValue;
		}

		#endregion

		#region angular drag sync

		[ReadOnly, SerializeField] private float _angularDrag;
		public float angularDrag {
			get => _angularDrag;
			set {
				OnAngularDragChanged(_angularDrag, value, false);
				target.angularDamping = _angularDrag = value;
				if (IsAuthority && IsServerInitialized)
					ObserversSetAngularDrag(value);
				else if (ClientWithAuthority)
					ServerSetAngularDrag(value);
			}
		}

		[ServerRpc]
		private void ServerSetAngularDrag(float value) {
			OnAngularDragChanged(_angularDrag, value, false);
			ObserversSetAngularDrag(value);
			_angularDrag = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetAngularDrag(float value) {
			OnAngularDragChanged(_angularDrag, value, false);
			_angularDrag = value;
		}

		private void OnAngularDragChanged(float _, float newValue, bool onServer) {
			if (IsAuthority) return;
			target.angularDamping = newValue;
		}

		#endregion

		#endregion
		
		/// <summary>
		/// Bool tracking if the "SyncVar"s have been initialized
		/// </summary>
		private bool isStarted;

		/// <summary>
		/// When we start the network connection make sure the network variables are synced
		/// </summary>
		public override void OnStartBoth() {
			base.OnStartBoth();

			UpdateOwnershipKinematicState();

			// Make sure that rarely updated properties have their initial values synced
			if (IsAuthority) {
				isKinematic = targetIsKinematic;
				gravityScale = target.gravityScale;
				drag = target.linearDamping;
				angularDrag = target.angularDamping;
			}

			isStarted = true;
		}

		/// <summary>
		/// When ownership of the object changes make sure properties are transferred
		/// </summary>
		/// <param name="prev"></param>
		public override void OnOwnershipBoth(NetworkConnection prev) {
			base.OnOwnershipBoth(prev);

			if (prev == Owner) return; // Ignore ownership changes if the owner didn't really change
			if (!isStarted) return;

			// If your the owner, make sure your local Rigidbody2D has the same settings as the previous owner
			if (IsAuthority) {
				target.linearVelocity = velocity;
				target.angularVelocity = angularVelocity;
			}

			UpdateOwnershipKinematicState();
		}
		
		/// <summary>
		/// Updates the kinematic state of rigidbodies, making sure that anyone without authority isn't performing physics calculations
		/// </summary>
		public void UpdateOwnershipKinematicState() {
			if (targetIsKinematic || !IsAuthority)
			{
				target.bodyType = RigidbodyType2D.Kinematic;
			}
			else
			{
				target.bodyType = RigidbodyType2D.Static;  // TODO is this the replacement for old isKinematic == false
			}
		}

		/// <summary>
		/// Every network tick, send data if we have authority
		/// </summary>
		public override void Tick() {
			if (!isActiveAndEnabled) return; // Only tick if we are enabled

			//Debug.Log($"Pre: {velocity} - {target.velocity}");

			SendDataIfAuthority();

			//Debug.Log($"Post: {velocity} - {target.velocity}");
		}

		// TODO: Should this be switched to occurring onPostTick?
		private void FixedUpdate() {
			if (clearAngularVelocity && !syncAngularVelocity) target.angularVelocity = 0;
			if (clearVelocity && !syncVelocity) target.linearVelocity = Vector2.zero;
		}


		/// <summary>
		/// Command to send values to server
		/// </summary>
		private void SendDataIfAuthority() {
			if (!IsAuthority) return;

			SendVelocity();
			SendRigidbody2DSettings();
		}

		/// <summary>
		/// Sends velocity values to the server
		/// </summary>
		private void SendVelocity() {
			// if angularVelocity has changed it is likely that velocity has also changed so just sync both values
			// however if only velocity has changed just send velocity
			if (syncVelocity && syncAngularVelocity) {
				velocity = target.linearVelocity;
				angularVelocity = target.angularVelocity;
				previousValue.velocity = target.linearVelocity;
				previousValue.angularVelocity = target.angularVelocity;
			} else if (syncVelocity) {
				velocity = target.linearVelocity;
				previousValue.velocity = target.linearVelocity;
			}
		}

		/// <summary>
		/// Sends other settings to the server if they have changed
		/// </summary>
		private void SendRigidbody2DSettings() {

			// These shouldn't change often so it is ok to send in their own Command
			if (previousValue.isKinematic != targetIsKinematic)
				previousValue.isKinematic = isKinematic = targetIsKinematic;

			if (previousValue.gravityScale != target.gravityScale)
				previousValue.gravityScale = gravityScale = target.gravityScale;

			if (Math.Abs(previousValue.drag - target.linearDamping) > Mathf.Epsilon)
				previousValue.drag = drag = target.linearDamping;

			if (Math.Abs(previousValue.angularDrag - target.angularDamping) > Mathf.Epsilon)
				previousValue.angularDrag = angularDrag = target.angularDamping;
		}

		/// <summary>
		///     holds previously synced values
		/// </summary>
		public struct ClientSyncState {
			public Vector2 velocity;
			public float angularVelocity;
			public bool isKinematic;
			public float gravityScale;
			public float drag;
			public float angularDrag;
		}
	}
}