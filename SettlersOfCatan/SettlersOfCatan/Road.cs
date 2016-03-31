﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace SettlersOfCatan
{
    public class Road : PictureBox
    {
        public int id;
        public Point position;

        List<Settlement> connectedSettlements=new List<Settlement>();

        private Player owningPlayer;

        public Road(Point position, int index)
        {
            this.position = position;
            BackColor = Color.Black;
            BackgroundImageLayout = ImageLayout.Stretch;
            Location = new Point(position.X - 6, position.Y - 6);
            Size = new Size(12, 12);

        }

        public Player getOwningPlayer()
        {
            return this.owningPlayer;
        }


        /**
        Returns true if the current road is already linked.
         */
        public bool linkSettlement(Settlement set)
        {
            foreach (Settlement currentSettlement in connectedSettlements)
            {
                if (set.id == currentSettlement.id)
                {
                    return true;
                }
            }
            connectedSettlements.Add(set);
            return false;
        }

        /**
            This algorithm is for checking if the player is able to build a road.
            Conditions for true:
                A- There must be a settlement owned by the player directly adjascent.
                B- There must be a road directly connected to this one owned by the player, but not blocked by another player.
         */
        public bool checkForConnection(Player currentPlayer)
        {
            foreach (Settlement set in connectedSettlements)
            {
                Player setPlayer = set.getOwningPlayer();
                //Is it the most likeley... no
                if (setPlayer == currentPlayer)
                {
                    return true;
                } else
                {
                    if (setPlayer != null)
                    {
                        return false; //Blocked by another player.
                    } else
                    {
                        //Check for a connected road with matching color.
                        List<Road> connectedRoads = set.getConnectedRoads();
                        foreach (Road r in connectedRoads) 
                        {
                            if (r.getOwningPlayer() == currentPlayer)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /**
            Add condition:
                Must have either a road or a settlement with matching player number.
         */
        public bool buildRoad(Player currentPlayer, bool takeResources)
        {
            if (owningPlayer == null)
            {
                if (
                    (takeResources && (currentPlayer.getResourceCount(Board.ResourceType.Wood) > 0 && currentPlayer.getResourceCount(Board.ResourceType.Brick) > 0)) 
                    || takeResources == false
                    )
                {
                    if (checkForConnection(currentPlayer))
                    {
                        this.owningPlayer = currentPlayer;
                        this.BackColor = currentPlayer.getPlayerColor();
                        if (takeResources)
                        {
                            /*
                                Take resources from player!
                             */
                        }
                        return true;
                    }
                } else
                {
                    MessageBox.Show("Not enough resources!");
                }
            }
            else
            {
                MessageBox.Show("Road is already built there!");
            }
            return false;
        }

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
