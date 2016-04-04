﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.Reflection;
using System.IO;

namespace SettlersOfCatan
{
    public partial class Board : Form
    {
        public static Bank TheBank = new Bank();

        public enum GameState {  Setup, DiceRoll, TieBreaker, PlayerOrder, FirstSettlement, PlayerTurn};
        public enum ResourceType { Wood=0, Brick, Ore, Wheat, Sheep, Desert};
        public String[] RESOURCE_NAMES = { "Wood", "Brick", "Ore", "Wheat", "Sheep", "NoResource" };
        public static String[] TILE_NAMES = { "Forest", "Hills", "Mountains", "Farms", "Fields", "Desert" };
        public Bitmap[] tileImages = new Bitmap[6];
        public String[] tileImageResourceNames = { "Forest_Tile.png", "Hills_Tile.png", "Mountain_Tile.png", "Wheat_Fields_Tile.png", "Pasture_Tile.png", "Desert_Tile.png" };

        //Keeps track of what tile indexes are ocean borders for later use.
        int[] oceanBorderInds = { 0, 1, 2, 3, 4, 8, 9, 14, 15, 21, 22, 27, 28, 32, 33, 34, 35, 36 };
        String[] tileFileNames = { "Rock.png", "Wood.png" };
        Random rand = new Random();
        Tile[] boardTiles = new Tile[BOARD_TILE_COUNT];
        List<Road> roadLocations = new List<Road>();
        List<Settlement> settlementLocations = new List<Settlement>();
        //This is the distribution of terrain resources for a four player game.
        public static Board.ResourceType[] fourPlayerTiles = 
            {
                Board.ResourceType.Ore, Board.ResourceType.Ore, Board.ResourceType.Ore,
                Board.ResourceType.Sheep, Board.ResourceType.Sheep, Board.ResourceType.Sheep, Board.ResourceType.Sheep,
                Board.ResourceType.Wood, Board.ResourceType.Wood, Board.ResourceType.Wood, Board.ResourceType.Wood,
                Board.ResourceType.Wheat, Board.ResourceType.Wheat, Board.ResourceType.Wheat, Board.ResourceType.Wheat,
                Board.ResourceType.Brick, Board.ResourceType.Brick, Board.ResourceType.Brick, Board.ResourceType.Desert
            };

        //This is the correctly ordered number chip distribution for a four player game.
        public int[] fourPlayerNumberChips = { 9,12,11,10,6,11,4,8,5,3,8,3,4,9,10,6,2,5};
        public Deck terrainTiles;
        public Deck numberChips;
        public static int BOARD_TILE_COUNT = 37;
        public static int SPACING = 128; //I require a 1:1 aspect ratio in order for FAR more simple positioning.
        //This variable is used to determine how many pixels tall the triangle of a terrain tile is.
        public static int TILE_TRIANGLE_HEIGHT = 35;

        Player[] playerPanels;
        Player[] playerOrder;
        public Player firstPlayer;
        int[] playerRolls = new int[4];
        public Player currentPlayer;
        public static GameState currentGameState;


        public Board()
        {
            InitializeComponent();

            this.pnlBoardArea.BackColor = Color.Transparent; //Set the color to transparent. The color is white by default so it is visible in the editor.
            this.pbBuildDevelopmentCard.MouseEnter += showDevelopmentCardToolTip;
            this.pbBuildDevelopmentCard.MouseLeave += hideDevelopmentCardToolTip;

            //Load up the tile image resources .
            for (int i = 0; i < 6; i++)
            {
                tileImages[i] = new Bitmap("Resources/" + tileImageResourceNames[i]);
            }
            //Set up player objects and initial player order
            playerOrder = new Player[4];
            playerPanels = new Player[4];
            playerPanels[0] = playerInfoPanel1;
            playerPanels[1] = playerInfoPanel2;
            playerPanels[2] = playerInfoPanel3;
            playerPanels[3] = playerInfoPanel4;
            for (int i = 0; i < playerPanels.Count(); i ++)
            {
                playerPanels[i].giveResource(new ResourceCard(ResourceType.Brick));
                playerPanels[i].giveResource(new ResourceCard(ResourceType.Wood));
                playerPanels[i].setPlayerNumber(i);

            }
            currentPlayer = playerPanels[0];

            currentGameState = GameState.Setup;

            distributeTiles();

            //Add the update event to all applicable controls
            boardUpdate(this, new EventArgs());
            this.dice.Click += boardUpdate;

        }

        public void distributeTiles()
        {

            //Sets up the terrain tiles deck
            terrainTiles = new Deck(19);
            foreach (Board.ResourceType r in fourPlayerTiles)
            {
                terrainTiles.putCard(new TerrainTile(pnlBoardArea, r, this.tileImages[(int)r]));

            }

            terrainTiles.shuffleDeck();

            //Set up the number chip deck. (can be randomized, however, the default is to keep the correct order).
            numberChips = new Deck(18);
            foreach (int num in fourPlayerNumberChips)
            {
                numberChips.putCard(new NumberChip(num));
            }

            /*
                If random number chip order is marked, randomize the number chips deck.
                    * Replace x with proper condition *
                if (x) {
                    numberChips.shuffleDeck();
                }
            */

            int row = 0;
            int column = 0;

            //This creates and positions each tile in the world.
            for (int i = 0; i < BOARD_TILE_COUNT; i++)
            {

                int numSettlementSpots = 2; //By default each tile creates 2 settlement spots

                //This checks for the end of a row on specific indices for correct row breaks.
                if (i == 4 || i == 9 || i == 15 || i == 22 || i == 28 || i == 33)
                {
                    row += 1;
                    column = 0;
                    //We need to create 3 settlement spots
                }
                //Shift is used to keep track of the row offsets for the correct "meshing" of the tiles.
                int shift = 0;
                //This checks for every other row and applies a half tile shift if it is an odd numbered row.
                if (row % 2 != 0)
                {
                    shift += SPACING / 2;
                }
                //This shifts the specified row indices by whole tiles for the correct look.
                switch (row)
                {
                    case 0:
                        shift += SPACING * 2;
                        break;
                    case 1:
                        shift += SPACING * 1;
                        break;
                    case 2:
                        shift += SPACING * 1;
                        break;
                    case 4:
                        shift += SPACING * 1;
                        break;
                    case 5:
                        shift += SPACING * 1;
                        //We need to create even more settlement spots (+4)
                        numSettlementSpots++;
                        //if (i==)
                        break;
                    case 6:
                        shift += SPACING * 2;
                        break;
                }

                //Determine if this tile is an ocean border.
                bool ocean = false;
                for (int f = 0; f < oceanBorderInds.Length; f++)
                {
                    if (i == oceanBorderInds[f])
                    {
                        ocean = true;
                    }
                }

                Point position = new Point(column * SPACING + shift, row * (SPACING - TILE_TRIANGLE_HEIGHT));

                if (!ocean)
                {
                    
                    TerrainTile tile = (TerrainTile)terrainTiles.drawTopCard();
                    tile.Location = position;
                    tile.index = i;
                    boardTiles[i] = tile;
                    if (tile.getResourceType() == ResourceType.Desert)
                    {
                        //Create a number chip with a value of 0
                        tile.setNumberChip(new NumberChip(0));
                        tile.placeThief();
                    } else
                    {
                        //Get the next number chip in line.
                        tile.setNumberChip((NumberChip)numberChips.drawTopCard());
                    }
                    
                } else
                {
                    OceanBorderTile tile = new OceanBorderTile(pnlBoardArea);
                    tile.Location = position;
                    tile.index = i;
                    boardTiles[i] = tile;
                }

                column++;

                /*
                    The following code block takes care of creating, linking, and positioning each settlement and road for the game.
                    This ensures there are no duplicate locations.

                */
                if (!ocean)
                {
                    TerrainTile t = (TerrainTile)boardTiles[i];
                    //Default placement is the top left and top center locations of the tile.
                    //This covers almost all settlement locations.

                    Point[] settlementPoints = new Point[6];                 
                    //A list of positions for each possible settlement location on this tile. The order matters greatly. (In a clockwise direction)
                    settlementPoints[0] = new Point(position.X, position.Y + TILE_TRIANGLE_HEIGHT);
                    settlementPoints[1] = new Point(position.X + (SPACING / 2), position.Y);
                    settlementPoints[2] = new Point(position.X + SPACING, position.Y + TILE_TRIANGLE_HEIGHT);
                    settlementPoints[3] = new Point(position.X + SPACING, position.Y + SPACING - TILE_TRIANGLE_HEIGHT);
                    settlementPoints[4] = new Point(position.X + (SPACING / 2), position.Y + SPACING);
                    settlementPoints[5] = new Point(position.X, position.Y + SPACING - TILE_TRIANGLE_HEIGHT);

                    for (int ind= 0;ind < 6;ind++)
                    {
                        //We check if this settlement location has already been created.
                        Settlement settlementLocation = findSettlementWithPosition(settlementPoints[ind]);
                        if (settlementLocation == null)
                        {
                            settlementLocation = new Settlement(settlementPoints[ind], i);
                            settlementLocations.Add(settlementLocation);
                            settlementLocation.MouseEnter += showSettlementBuildToolTip;
                            settlementLocation.MouseLeave += hideSettlementBuildToolTip;
                            settlementLocation.id = settlementLocations.Count;
                            pnlBoardArea.Controls.Add(settlementLocation);
                            settlementLocation.BringToFront();
                        }
                        t.adjascentSettlements.Add(settlementLocation);
                    }


                    //This is almost an exact duplicate of the above process. The only difference being the positions.
                    Point[] roadPoints = new Point[6];
                    roadPoints[0] = new Point(position.X, position.Y + (SPACING / 2));
                    roadPoints[1] = new Point(position.X + (SPACING / 4), position.Y + (TILE_TRIANGLE_HEIGHT / 2));
                    roadPoints[2] = new Point(position.X + (SPACING / 4) * 3, position.Y + (TILE_TRIANGLE_HEIGHT / 2));
                    roadPoints[3] = new Point(position.X + SPACING, position.Y + (SPACING / 2));
                    roadPoints[4] = new Point(position.X + (SPACING / 4) * 3, position.Y + (SPACING - (TILE_TRIANGLE_HEIGHT / 2)));
                    roadPoints[5] = new Point(position.X + (SPACING / 4), position.Y + (SPACING - (TILE_TRIANGLE_HEIGHT / 2)));
                    foreach (Point roadPoint in roadPoints)
                    {
                        //We check if this settlement location has already been created.
                        Road roadLocation = findRoadWithPosition(roadPoint);
                        if (roadLocation == null)
                        {
                            roadLocation = new Road(roadPoint, i);
                            pnlBoardArea.Controls.Add(roadLocation);
                            roadLocation.MouseEnter += showRoadBuildToolTip;
                            roadLocation.MouseLeave += hideRoadBuildToolTip;
                            roadLocation.id = roadLocations.Count;
                            roadLocation.Click += this.buildRoad;
                            roadLocations.Add(roadLocation);
                            roadLocation.BringToFront();
                        }
                        t.adjascentRoads.Add(roadLocation);
                    }

                    //Next we link the roads and settlements together.
                    //We know the order in which the roads and settlements were created in, so all we need to do is link them in that order.
                    for (int index = 0; index < 6; index++)
                    {
                        t.adjascentRoads[index].linkSettlement(t.adjascentSettlements[index]);
                        //Link settlement to road
                        if (index==0)
                        {   
                            t.adjascentRoads[index].linkSettlement(t.adjascentSettlements[5]);
                        } else
                        {
                            t.adjascentRoads[index].linkSettlement(t.adjascentSettlements[index - 1]);
                        }
                        //Link road to settlement
                        t.adjascentSettlements[index].linkRoad(t.adjascentRoads[index]);
                        if (index==5)
                        {
                            t.adjascentSettlements[index].linkRoad(t.adjascentRoads[0]);
                        }
                        else
                        {
                            t.adjascentSettlements[index].linkRoad(t.adjascentRoads[index + 1]);
                        }
                    }
                }
            }
        }

        /*
            This function controls the flow of the game.
            Any control that changes the state of the game has this function added to it's click event.
         */
        public void boardUpdate(Object sender, EventArgs e)
        {
            //MessageBox.Show(sender.GetType().ToString());
            switch (currentGameState)
            {
                case GameState.Setup:
                    addEventText("Welcome to the world of Catan!");
                    currentGameState = GameState.DiceRoll;
                    break;
                case GameState.DiceRoll:
                    //This will only be executed if the sending object is dice.
                    if (sender is Dice)
                    {
                        Player p = this.currentPlayer;
                        //Cast the sender object as dice.
                        int roll = ((Dice)sender).getRollValue();
                        addEventText(p.getPlayerName() + " rolled a " + roll + ".");
                        //Check if the last roll (previous player's roll) is less than the current. (first will always be greater).
                        this.playerRolls[p.getPlayerNumber()] = roll;
                        //If the current player is the last player in the list.
                        if (currentPlayer.getPlayerNumber() >= playerPanels.Count()-1)
                        {
                            //Check for a tie
                            bool matchedHigh = false;
                            int high = 0;
                            for (int i = 0; i < playerOrder.Count(); i ++)
                            {
                                //Pick the greater number.
                                if (high == playerRolls[i])
                                {
                                    matchedHigh = true;
                                } else if (high < playerRolls[i])
                                {
                                    matchedHigh = false;
                                    high = playerRolls[i];
                                }
                            }
                            if (matchedHigh == true)
                            {
                                addEventText("There was a tie between: ");
                                //We have a tie here!
                                currentGameState = GameState.PlayerOrder;
                                //What players roll to break the tie?
                                int ct = 0;
                                for (int i = 0; i < playerOrder.Count(); i ++ )
                                {
                                    //Get the players with rolls equal to high
                                    if (playerRolls[i] == high)
                                    {
                                        addEventText(playerPanels[i].getPlayerName() + "");
                                        ct++;
                                    }
                                }
                            }
                            else
                            {
                                //All is good carry on.
                                currentGameState = GameState.PlayerOrder;
                            }

                            boardUpdate(sender, e);
                            break;
                        }

                        currentPlayer = playerPanels[p.getPlayerNumber() + 1];
                    }
                    break;
                case GameState.TieBreaker:

                    break;
                case GameState.PlayerOrder:
                    //Determine the winner of the roll.
                    int highn = 0;
                    for (int i = 0; i < playerOrder.Count(); i ++ )
                    {
                        if (highn < playerRolls[i])
                        {
                            highn = playerRolls[i];
                            playerOrder[0] = playerPanels[i];
                        }
                    }
                    addEventText(playerOrder[0].getPlayerName() + " won the roll and is the first player.");
                    this.firstPlayer = playerOrder[0];
                    //Add players to turn order...
                    //Add the remainder of the players in order to the list.
                    int fp = playerOrder[0].getPlayerNumber();

                    for (int i = 0; i < playerOrder.Count(); i++)
                    {
                        playerOrder[i] = playerPanels[fp];
                        addEventText("Player " + i + ": " + playerPanels[fp].getPlayerName());
                        fp++;
                        if (fp == playerOrder.Count())
                        {
                            fp = 0;
                        }
                    }
                    //Move to the next setup state.
                    firstPlayer.setTurn(true);
                    addEventText(playerOrder[0].getPlayerName() + " please place your first settlement.");
                    currentGameState = GameState.FirstSettlement;
                    break;
                case GameState.FirstSettlement:
                    //Allow each player to place a settlement
                    break;
                case GameState.PlayerTurn:
                    break;
            }
            if (currentGameState == GameState.Setup)
            {

                /*
                    Settlement and road process
                 */

                /*
                    First resource gather process.
                 */
            }
        }

        /**
            Checks if the points are equal.
         */
        public bool LocationsEqual(Point p1, Point p2)
        {
            return (p1.X == p2.X) && (p1.Y == p2.Y);
        }

        /**
            Used to determine if the distance between two points is less than the provided value.
            Uses the distance formula A*A - B*B = C*C (Pythagorean theorem)
         */
        public bool distanceLessThan(Point p1, Point p2, int distance)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)) < distance;
        }

        /**
            This will look in the list of road locations for a road with a *roughly* matching location and return it.
            We use a distance formula to determine if the position is within our margin of error due to small
            rounding differences between the different road locations. (4 is a bit much for rounding errors)
         */
        public Road findRoadWithPosition(Point pt)
        {
            Road r = null;

            foreach (Road road in roadLocations)
            {
                if (distanceLessThan(pt, road.position, 4))
                {
                    r = road;
                }
            }
            return r;
        }

        /**
           This will look in the list of settlement locations for a settlement with a matching location then return it. 
         */
        public Settlement findSettlementWithPosition(Point pt)
        {
            Settlement s = null;
            foreach (Settlement settlement in settlementLocations)
            {
                //MessageBox.Show(pt.X + " " + pt.Y + settlement.Location.X + " " + settlement.Location.Y);
                if (LocationsEqual(pt, settlement.position))
                {
                    s = settlement;
                }
            }
            return s;
        }

        /*
            Used to call the road build function in road while providing the current player.
         */
        public void buildRoad(object sender, EventArgs e)
        {
            bool takeResources = false;
            if (currentGameState == GameState.PlayerTurn)
            {
                takeResources = true;
            }
            bool success = ((Road)sender).buildRoad(this.currentPlayer, takeResources);
            //For the game start thingey....
        }

        public void buildSettlement(object sender, EventArgs e)
        {

        }


        //Tool tip events.

        public void showRoadBuildToolTip(object sender, EventArgs e)
        {
            if (Board.currentGameState == GameState.PlayerTurn)
            {
                Point loc = ((PictureBox)sender).Location;
                loc.X += 32;
                loc.Y += 32;
                this.pnlRoadToolTip.Location = loc;
                this.pnlRoadToolTip.Visible = true;
            }
        }

        public void hideRoadBuildToolTip(object sender, EventArgs e)
        {
            this.pnlRoadToolTip.Visible = false;
        }

        public void showSettlementBuildToolTip(object sender, EventArgs s)
        {
            if (Board.currentGameState == GameState.PlayerTurn)
            {
                Point loc = ((PictureBox)sender).Location;
                loc.X += 32;
                loc.Y += 32;
                this.pnlSettlementToolTip.Location = loc;
                this.pnlSettlementToolTip.Visible = true;
            }
        }

        public void hideSettlementBuildToolTip(object sender, EventArgs s)
        {
            this.pnlSettlementToolTip.Visible = false;
        }


        public void showDevelopmentCardToolTip(object sender, EventArgs s)
        {
            if (Board.currentGameState == GameState.PlayerTurn)
            {
                Point loc = ((PictureBox)sender).Parent.Location;
                loc.X += ((PictureBox)sender).Width;
                loc.Y += ((PictureBox)sender).Location.Y;
                this.pnlDevelopmentCardToolTip.Location = loc;
                this.pnlDevelopmentCardToolTip.Visible = true;
            }
        }

        public void hideDevelopmentCardToolTip(object sender, EventArgs s)
        {
            this.pnlDevelopmentCardToolTip.Visible = false;
        }

        /**
            Adds the text to the events list box and scrolls it to the bottom, then hides the blue highlight bar.
         */
        public void addEventText(String text)
        {
            this.lstGameEvents.Items.Add(text);
            this.lstGameEvents.SelectedIndex = this.lstGameEvents.Items.Count - 1;
            this.lstGameEvents.SelectedIndex =  -1;
        }
    }
}
