﻿using System.Collections.Generic;
using Assets.Scripts.GameMechanic;
using Assets.Scripts.GameMechanic.Commands;
using Assets.Scripts.Interface.InterfaceCommand;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Interface
{
    public class InterfaceCommandController:MonoBehaviour
    {
        private BaseInterfaceCommand currentCommand;

        public List<BaseInterfaceCommand> AllCommands;

        private InterfaceCommandMove moveCommand;
        public InterfaceCommandIdle IdleCommand;

        private UnitCommandController unitCommandController;

        public BaseInterfaceCommand SelectedCommand;

        public Projector SelectorProjector;

        void Start()
        {
            foreach (var command in AllCommands)
            {
                if (command is InterfaceCommandMove)
                {
                    moveCommand = command as InterfaceCommandMove;
                }
            }

            Game.InterfaceCommandController = this;
            unitCommandController = Game.PlayerUnit.GetComponent<UnitCommandController>();

            SelectorProjector.transform.parent.SetParent(Game.PlayerUnit.transform);
        }

        void Update()
        {
            if ((unitCommandController.CurrentCommand == null) || (unitCommandController.CurrentCommand is IdleCommand))
            {
                currentCommand = IdleCommand;
                IdleCommand.OnTargetSelected();
            }

            if (currentCommand != null)
            {
                currentCommand.UpdateCommand();
                currentCommand.SelectAvailableCommands(AllCommands);
                UpdateCommandButtons();
            }

            SelectorProjector.transform.parent.gameObject.SetActive((SelectedCommand != null) && SelectedCommand.ShowSelector);

            if (SelectedCommand != null)
            {
                SelectedCommand.UpdateSelection();

                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100))
                {
                    Vector2 from = new Vector2(Vector3.forward.x, Vector3.forward.z);
                    Vector2 to = new Vector2((hit.point - Game.PlayerUnit.transform.position).x, (hit.point - Game.PlayerUnit.transform.position).z);

                    SelectorProjector.transform.parent.eulerAngles = new Vector3(0, -Vector2.SignedAngle(from, to), 0);
                }

                SelectorProjector.aspectRatio = SelectedCommand.SelectorDistance/2;
                SelectorProjector.transform.localPosition = new Vector3(0,0, SelectedCommand.SelectorDistance / 2);

                if (SelectedCommand.State != CommandState.TargetSelection)
                {
                    SelectedCommand = null;
                }
            }

            LeftClickHolder();
            SpaceDownHolder();
        }

        public void RunCommand(BaseInterfaceCommand newCommand)
        {
            currentCommand = newCommand;
        }

        public void SelectCommand(BaseInterfaceCommand command)
        {
            if (command == SelectedCommand)
                return;

            if (SelectedCommand != null)
            {
                if (SelectedCommand == currentCommand)
                {
                    SelectedCommand.State = CommandState.Active;
                }
                else
                {
                    SelectedCommand.State = CommandState.Available;
                }
            }

            SelectedCommand = command;
            SelectedCommand.State = CommandState.TargetSelection;
            SelectedCommand.OnSelectCommand();
        }

        public void LeftClickHolder()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            

            if (Input.GetButtonDown("Fire1"))
            {
                Ray ray =  UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100))
                {
                    if (hit.transform.gameObject.CompareTag("Enemy"))
                    {
                        if (SelectedCommand == null)
                        {
                            SelectDeselectTarget(hit.transform.gameObject);
                        }
                        else
                        {
                            ClickPointToCommand(hit.transform.position);
                        }
                    }
                    else
                    {
                        ClickPointToCommand(hit.point);
                    }
                }
            }
        }

        private void ClickPointToCommand(Vector3 point)
        {
            if (SelectedCommand == null)
            {
                if (unitCommandController.CurrentCommand == null || unitCommandController.CurrentCommand.CanBeInterruptedByCommand(new MoveCommand(point, Game.PlayerUnit)))
                {
                    SelectedCommand = moveCommand;
                    SelectedCommand.OnTerrainClick(point);
                }
            }
            else
            {
                SelectedCommand.OnTerrainClick(point);
            }
        }

        private void UpdateCommandButtons()
        {
            foreach (var command in AllCommands)
            {
                if ((command.State == CommandState.Hidden) || (command.StaminaCost>Game.PlayerUnit.Stamina))
                {
                    command.CommandButton.gameObject.SetActive(false);
                }
                else
                {
                    command.CommandButton.gameObject.SetActive(true);
                }
            }
        }

        public void SelectDeselectTarget(GameObject target)
        {
            GameUnit targetUnit = target.GetComponent<GameUnit>();
            if ((Game.PlayerUnit.Target == null) || (Game.PlayerUnit.Target != targetUnit))
            {
                Game.PlayerUnit.Target = targetUnit;
            }
            else
            {
                Game.PlayerUnit.Target = null;
            }
        }

        public void SpaceDownHolder()
        {
            if (Input.GetButtonDown("Jump"))
            {
                Game.GameTime.SwitchPause();
            }
        }
    }
}