﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ARKBreedingStats
{
    public partial class CreatureBox : UserControl
    {
        private Creature creature;
        private NumericUpDown[] numUDLevelsDom;
        public delegate void ChangedEventHandler(Creature creature, bool creatureStatusChanged);
        public event ChangedEventHandler Changed;
        public delegate void EventHandler(object sender, Creature creature);
        public event EventHandler GiveParents;
        public event EventHandler EditCreature;
        private Sex sex;
        private CreatureStatus status;
        public List<Creature>[] parentList; // all creatures that could be parents (i.e. same species, separated by sex)
        public List<int>[] parentListSimilarity; // for all possible parents the number of equal stats (to find the parents easier)
        private bool[] colorRegionUseds;
        private Image largeImage;
        private bool renewLargeImage;
        public int maxDomLevel = 0;
        ToolTip tt = new ToolTip();

        public CreatureBox()
        {
            initializeVars();
        }

        public CreatureBox(Creature creature)
        {
            initializeVars();
            setCreature(creature);
        }

        private void initializeVars()
        {
            InitializeComponent();
            creature = null;
            numUDLevelsDom = new[] { numericUpDown1, numericUpDown2, numericUpDown3, numericUpDown4, numericUpDown5, numericUpDown6, numericUpDown7 };
            parentComboBoxMother.naLabel = "- Mother n/a";
            parentComboBoxFather.naLabel = "- Father n/a";
            regionColorChooser1.RegionColorChosen += RegionColorChooser1_RegionColorChosen;

            // tooltips
            tt.SetToolTip(labelHeaderDomLevelSet, "Set the spend domesticated Levels here");
            tt.SetToolTip(buttonEdit, "Edit");
            tt.SetToolTip(labelM, "Mother");
            tt.SetToolTip(labelF, "Father");
            tt.SetToolTip(textBoxNote, "Note");
            tt.SetToolTip(labelParents, "Mother and Father (if bred and choosen)");
            tt.SetToolTip(buttonSex, "Sex");
            tt.SetToolTip(buttonStatus, "Status: Available, Unavailable, Dead");
        }

        public void setCreature(Creature creature)
        {
            Clear();
            this.creature = creature;
            regionColorChooser1.setCreature(creature.species, creature.colors);
            colorRegionUseds = regionColorChooser1.ColorRegionsUseds;

            updateLabel();
            renewLargeImage = true;
        }

        public int BarMaxLevel
        {
            set => statsDisplay1.BarMaxLevel = value;
        }

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            SuspendLayout();
            if (creature != null)
            {
                if (panel1.Visible)
                {
                    closeSettings(false);
                }
                else
                {
                    checkBoxIsBred.Checked = creature.isBred;
                    panelParents.Visible = creature.isBred;
                    if (creature.isBred)
                        populateParentsList();
                    textBoxName.Text = creature.name;
                    textBoxOwner.Text = creature.owner;
                    textBoxNote.Text = creature.note;
                    sex = creature.sex;
                    buttonSex.Text = Utils.sexSymbol(sex);
                    status = creature.status;
                    buttonStatus.Text = Utils.statusSymbol(status);
                    textBoxName.SelectAll();
                    textBoxName.Focus();
                    panel1.Visible = true;
                    for (int s = 0; s < 7; s++)
                    {
                        numUDLevelsDom[s].Value = creature.levelsDom[s];
                    }
                }
            }
            ResumeLayout();
        }

        private void populateParentsList()
        {
            if (parentList[0] == null || parentList[1] == null)
            {
                GiveParents(this, creature);

                parentComboBoxMother.preselectedCreatureGuid = creature.motherGuid;
                parentComboBoxFather.preselectedCreatureGuid = creature.fatherGuid;
                parentComboBoxMother.parentsSimilarity = parentListSimilarity[0];
                parentComboBoxMother.ParentList = parentList[0];
                parentComboBoxFather.parentsSimilarity = parentListSimilarity[1];
                parentComboBoxFather.ParentList = parentList[1];
            }
        }

        public void updateLabel()
        {
            if (creature != null)
            {
                groupBox1.Text = $"{creature.name} (Lvl {creature.level}/{creature.levelHatched + maxDomLevel})";
                if (creature.Mother != null || creature.Father != null)
                {
                    labelParents.Text = "";
                    if (creature.Mother != null)
                        labelParents.Text = "Mo: " + creature.Mother.name;
                    if (creature.Father != null && creature.Mother != null)
                        labelParents.Text += "; ";
                    if (creature.Father != null)
                        labelParents.Text += "Fa: " + creature.Father.name;
                }
                else if (creature.isBred)
                {
                    labelParents.Text = "bred, click 'edit' to add parents";
                }
                else
                {
                    labelParents.Text = "found wild " + creature.levelFound + (creature.tamingEff >= 0 ? ", tamed with TE: " + (creature.tamingEff * 100).ToString("N1") + "%" : ", TE unknown.");
                }
                statsDisplay1.SetCreatureValues(creature);
                labelNotes.Text = creature.note;
                labelSpecies.Text = creature.species;
                pictureBox1.Image = CreatureColored.getColoredCreature(creature.colors, creature.species, colorRegionUseds);
                tt.SetToolTip(pictureBox1, CreatureColored.RegionColorInfo(creature.species, creature.colors));
                pictureBox1.Visible = true;
            }
        }

        private void closeSettings(bool save)
        {
            panel1.Visible = false;
            if (save)
            {
                SuspendLayout();
                creature.name = textBoxName.Text;
                creature.sex = sex;
                creature.owner = textBoxOwner.Text;
                Creature parent = null;
                if (checkBoxIsBred.Checked)
                    parent = parentComboBoxMother.SelectedParent;
                creature.motherGuid = parent?.guid ?? Guid.Empty;
                bool parentsChanged = false;
                if (creature.Mother != parent)
                {
                    creature.Mother = parent;
                    parentsChanged = true;
                }
                parent = null;
                if (checkBoxIsBred.Checked)
                    parent = parentComboBoxFather.SelectedParent;
                creature.fatherGuid = parent?.guid ?? Guid.Empty;
                if (creature.Father != parent)
                {
                    creature.Father = parent;
                    parentsChanged = true;
                }
                if (parentsChanged)
                    creature.recalculateAncestorGenerations();

                creature.isBred = checkBoxIsBred.Checked;

                for (int s = 0; s < 7; s++)
                {
                    creature.levelsDom[s] = (int)numUDLevelsDom[s].Value;
                }
                creature.note = textBoxNote.Text;
                bool creatureStatusChanged = (creature.status != status);
                creature.status = status;

                Changed(creature, creatureStatusChanged);
                updateLabel();
                ResumeLayout();
            }
        }

        // call this function to clear all contents of this element
        public void Clear()
        {
            parentComboBoxMother.Items.Clear();
            parentComboBoxFather.Items.Clear();
            parentList = new List<Creature>[2];
            closeSettings(false);
            groupBox1.Text = "";
            creature = null;
            statsDisplay1.Clear();
            pictureBox1.Visible = false;
            regionColorChooser1.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            closeSettings(true);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            closeSettings(false);
        }

        private void buttonSex_Click(object sender, EventArgs e)
        {
            sex = Utils.nextSex(sex);
            buttonSex.Text = Utils.sexSymbol(sex);
        }

        private void buttonStatus_Click(object sender, EventArgs e)
        {
            status = Utils.nextStatus(status);
            buttonStatus.Text = Utils.statusSymbol(status);
        }

        private void checkBoxIsBred_CheckedChanged(object sender, EventArgs e)
        {
            panelParents.Visible = checkBoxIsBred.Checked;
            if (checkBoxIsBred.Checked)
                populateParentsList();
        }

        private void RegionColorChooser1_RegionColorChosen()
        {
            creature.colors = regionColorChooser1.colorIDs;
            pictureBox1.Image = CreatureColored.getColoredCreature(creature.colors, creature.species, colorRegionUseds);
            renewLargeImage = true;
            Changed(creature, false);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (renewLargeImage)
            {
                largeImage = CreatureColored.getColoredCreature(creature.colors, creature.species, colorRegionUseds, 256);
                renewLargeImage = false;
            }
        }

        private void buttonEditMore_Click(object sender, EventArgs e)
        {
            EditCreature?.Invoke(this, creature);
        }
    }
}