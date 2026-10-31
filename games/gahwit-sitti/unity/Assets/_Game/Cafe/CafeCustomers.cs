using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gahwit.Cafe
{
    // The customers in the 3D café, as simple figures until the character art arrives (missing/13). Each order is a
    // customer (DEVLOG 2026-10-31): they walk in through the door, sit if a chair is free or wait by the counter,
    // sip when served and walk out. Figures are pooled. The rules are in Customers.CustomerRules.
    public sealed class CafeCustomers : MonoBehaviour
    {
        const float WalkSpeed = 1.3f, SipSeconds = 2.2f, SeatDrop = 0.35f;

        public struct Info
        {
            public int OrderId;
            public Color Color;
        }

        enum Phase { Entering, Waiting, Sipping, Leaving }

        sealed class Figure
        {
            public int OrderId;
            public Transform Root, Head;
            public Renderer Body;
            public Phase Phase;
            public readonly List<Vector3> Path = new List<Vector3>();
            public int Seat = -1, Spot = -1;
            public float SipStarted;
        }

        readonly List<Figure> active = new List<Figure>();
        readonly Stack<Figure> pool = new Stack<Figure>();
        readonly HashSet<int> present = new HashSet<int>();
        CafeRoom room;
        Material template;
        Material skin;
        int seats;

        public event Action<int> Arrived; // order id: someone walks in

        public void Init(CafeRoom room)
        {
            this.room = room;
            template = Resources.Load<Material>("Materials/CafeLit");
            skin = new Material(template);
            skin.SetColor("_BaseColor", new Color(0.86f, 0.69f, 0.58f));
        }

        // Matches the figures to the orders: new orders walk in, orders that are gone walk out.
        public void Sync(IReadOnlyList<Info> customers, int freeSeats)
        {
            seats = freeSeats;
            present.Clear();
            foreach (var info in customers)
            {
                present.Add(info.OrderId);
                if (Find(info.OrderId) == null) Enter(info);
            }
            foreach (var figure in active)
                if (!present.Contains(figure.OrderId) && (figure.Phase == Phase.Entering || figure.Phase == Phase.Waiting)) Leave(figure);
        }

        public void Serve(int orderId)
        {
            var figure = Find(orderId);
            if (figure == null) return;
            figure.Phase = Phase.Sipping;
            figure.SipStarted = Time.time;
        }

        public bool TryHead(int orderId, out Vector3 world)
        {
            var figure = Find(orderId);
            world = figure != null && figure.Phase == Phase.Waiting ? figure.Head.position + Vector3.up * 0.45f : default;
            return figure != null && figure.Phase == Phase.Waiting;
        }

        Figure Find(int orderId)
        {
            foreach (var figure in active)
                if (figure.OrderId == orderId) return figure;
            return null;
        }

        void Enter(Info info)
        {
            var figure = pool.Count > 0 ? pool.Pop() : Build();
            figure.OrderId = info.OrderId;
            figure.Body.material.SetColor("_BaseColor", info.Color);
            figure.Root.gameObject.SetActive(true);
            figure.Root.position = room.DoorOutside;
            figure.Seat = FreeSeat();
            figure.Spot = figure.Seat < 0 ? FreeSpot() : -1;
            figure.Path.Clear();
            figure.Path.Add(room.DoorInside);
            figure.Path.Add(figure.Seat >= 0 ? room.Seat(figure.Seat) : room.WaitingSpot(figure.Spot));
            figure.Phase = Phase.Entering;
            active.Add(figure);
            Arrived?.Invoke(info.OrderId);
        }

        void Leave(Figure figure)
        {
            figure.Path.Clear();
            figure.Path.Add(room.DoorInside);
            figure.Path.Add(room.DoorOutside);
            figure.Phase = Phase.Leaving;
            figure.Seat = figure.Spot = -1;
        }

        int FreeSeat()
        {
            for (int i = 0; i < seats; i++)
                if (!active.Exists(f => f.Seat == i)) return i;
            return -1;
        }

        int FreeSpot()
        {
            for (int i = 0; ; i++)
                if (!active.Exists(f => f.Spot == i)) return i;
        }

        Figure Build()
        {
            var root = new GameObject("Customer").transform;
            root.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0, 0.75f, 0);
            body.transform.localScale = new Vector3(0.44f, 0.55f, 0.44f);
            var renderer = body.GetComponent<Renderer>();
            renderer.material = new Material(template);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root, false);
            head.transform.localPosition = new Vector3(0, 1.5f, 0);
            head.transform.localScale = Vector3.one * 0.34f;
            head.GetComponent<Renderer>().sharedMaterial = skin;
            return new Figure { Root = root, Head = head.transform, Body = renderer };
        }

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var figure = active[i];
                switch (figure.Phase)
                {
                    case Phase.Entering:
                    case Phase.Leaving:
                        if (Walk(figure)) Arrive(figure, i);
                        break;
                    case Phase.Waiting:
                        // Breathing, and sitting lower on a chair.
                        figure.Head.localPosition = new Vector3(0, 1.5f + 0.015f * Mathf.Sin(Time.time * 2f + figure.OrderId), 0);
                        break;
                    case Phase.Sipping:
                        float t = (Time.time - figure.SipStarted) / SipSeconds;
                        figure.Head.localPosition = new Vector3(0, 1.5f - 0.06f * Mathf.Sin(t * Mathf.PI * 3f), 0);
                        if (t >= 1f) Leave(figure);
                        break;
                }
            }
        }

        // Moves towards the next point; true once the path is walked.
        bool Walk(Figure figure)
        {
            if (figure.Path.Count == 0) return true;
            var position = figure.Root.position;
            position.y = 0;
            var target = figure.Path[0];
            var step = Vector3.MoveTowards(position, target, WalkSpeed * Time.deltaTime);
            var direction = target - position;
            if (direction.sqrMagnitude > 0.0001f)
                figure.Root.rotation = Quaternion.Slerp(figure.Root.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            figure.Root.position = step + Vector3.up * (0.04f * Mathf.Abs(Mathf.Sin(Time.time * 9f)));
            if ((step - target).sqrMagnitude < 0.0004f) figure.Path.RemoveAt(0);
            return figure.Path.Count == 0;
        }

        void Arrive(Figure figure, int index)
        {
            if (figure.Phase == Phase.Leaving)
            {
                figure.Root.gameObject.SetActive(false);
                active.RemoveAt(index);
                pool.Push(figure);
                return;
            }
            figure.Phase = Phase.Waiting;
            if (figure.Seat >= 0)
            {
                // Sit facing the table.
                var table = room.TableOf(figure.Seat);
                var seat = room.Seat(figure.Seat);
                figure.Root.rotation = Quaternion.LookRotation(table - seat);
                figure.Root.position = seat + Vector3.down * SeatDrop + Vector3.up * 0.46f;
            }
            else
            {
                figure.Root.position = room.WaitingSpot(figure.Spot);
                figure.Root.rotation = Quaternion.LookRotation(Vector3.back); // towards the counter
            }
        }
    }
}
