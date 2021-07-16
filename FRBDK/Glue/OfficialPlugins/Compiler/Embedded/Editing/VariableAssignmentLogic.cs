﻿{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FlatRedBall.Math.Collision;
using System.Collections;
using GlueControl.Dtos;


namespace GlueControl.Editing
{
    public static class VariableAssignmentLogic
    {
        public static GlueVariableSetDataResponse SetVariable(GlueVariableSetData data, PositionedObject forcedItem = null)
        {
            object variableValue = ConvertVariableValue(data);

            var response = new GlueVariableSetDataResponse();

            try
            {

                var screen =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen;

                var elementGameType = data.InstanceOwnerGameType;
                var ownerType = typeof(VariableAssignmentLogic).Assembly.GetType(data.InstanceOwnerGameType);
                Models.GlueElement ownerElement = null;
                if (InstanceLogic.Self.CustomGlueElements.ContainsKey(elementGameType))
                {
                    ownerElement = InstanceLogic.Self.CustomGlueElements[elementGameType];
                }


                var setOnEntity =
                    (ownerType != null && typeof(PositionedObject).IsAssignableFrom(ownerType))
                    ||
                    ownerElement is Models.EntitySave;

                if(setOnEntity)
                {
                    var variableNameOnObjectInInstance = data.VariableName.Substring("this.".Length);
                    if(forcedItem != null)
                    {
                        if (CommandReceiver.DoTypesMatch(forcedItem, data.InstanceOwnerGameType, ownerType))
                        {
                            screen.ApplyVariable(variableNameOnObjectInInstance, variableValue, forcedItem);
                        }
                    }
                    else
                    {
                        // Loop through all objects in the SpriteManager. If we are viewing a single 
                        // entity in the entity screen, then this will only loop 1 time and will set 1 value.
                        // If we are in a screen where multiple instances of the entity are around, then we set the 
                        // value on all instances
                        foreach (var item in SpriteManager.ManagedPositionedObjects)
                        {
                            if(CommandReceiver.DoTypesMatch(item, data.InstanceOwnerGameType, ownerType))
                            {
                                screen.ApplyVariable(variableNameOnObjectInInstance, variableValue, item);
                            }
                        }
                        response.WasVariableAssigned = true;
                    }
                }
                else
                {
                    variableValue = SetValueOnObjectInScreen(data, variableValue, response, screen);
                }
            }
            catch(Exception e)
            {
                response.Exception = e.ToString();
                response.WasVariableAssigned = false;
            }
            return response;
        }

        private static object SetValueOnObjectInScreen(GlueVariableSetData data, object variableValue, GlueVariableSetDataResponse response, FlatRedBall.Screens.Screen screen)
        {
            response.WasVariableAssigned = false;
            var splitVariable = data.VariableName.Split('.');

            try
            {
                object targetInstance = GetTargetInstance(data, ref variableValue, screen);

                if (targetInstance is CollisionRelationship && splitVariable[2] == "Entire CollisionRelationship")
                {
                    response.WasVariableAssigned = TryAssignCollisionRelationship(splitVariable[1],
                        JsonConvert.DeserializeObject<Models.NamedObjectSave>(data.VariableValue));
                }
                else if (targetInstance is FlatRedBall.TileCollisions.TileShapeCollection && splitVariable[2] == "Entire TileShapeCollection")
                {
                    response.WasVariableAssigned = TryAssignTileShapeCollection(splitVariable[1],
                        JsonConvert.DeserializeObject<Models.NamedObjectSave>(data.VariableValue));
                }
                else if (targetInstance != null)
                {
                    response.WasVariableAssigned = screen.ApplyVariable(splitVariable[2], variableValue, targetInstance);
                }
            }
            catch (Exception e)
            {
                response.WasVariableAssigned = false;
                response.Exception = e.ToString();
            }


            return variableValue;
        }

        private static object GetTargetInstance(GlueVariableSetData data, ref object variableValue, FlatRedBall.Screens.Screen screen)
        {
            var splitVariable = data.VariableName.Split('.');

            object targetInstance = null;


            // this searches for a name. we need to force it on a type too so newly-added objects can have their variables set....

            if (splitVariable[0] == "this" && splitVariable.Length > 1)
            {

                var aarect = ShapeManager.VisibleRectangles.FirstOrDefault(item =>
                    item.Parent == null &&
                    item.Name == splitVariable[1]);
                if (aarect != null)
                {
                    targetInstance = aarect;
                }

                if (targetInstance == null)
                {
                    var circle = ShapeManager.VisibleCircles.FirstOrDefault(item =>
                        item.Parent == null &&
                        item.Name == splitVariable[1]);
                    if (circle != null)
                    {
                        targetInstance = circle;
                    }
                }

                if (targetInstance == null)
                {
                    var polygon = ShapeManager.VisiblePolygons.FirstOrDefault(item =>
                        item.Parent == null &&
                        item.Name == splitVariable[1]);

                    if (polygon != null)
                    {
                        targetInstance = polygon;
                        if (splitVariable[2] == "Points" && variableValue is List<Microsoft.Xna.Framework.Vector2> vectorList)
                        {
                            variableValue = vectorList.Select(item => new FlatRedBall.Math.Geometry.Point(item.X, item.Y)).ToList();
                        }
                    }
                }

                if (targetInstance == null)
                {
                    var sprite = SpriteManager.AutomaticallyUpdatedSprites.FirstOrDefault(item =>
                        item.Parent == null &&
                        item.Name == splitVariable[1]);

                    if (sprite != null)
                    {
                        targetInstance = sprite;
                    }
                }

                // Try "Entire CollisionRelationship" first, and if not, do the normal assignment
                if (targetInstance == null)
                {
                    if (splitVariable[2] == "Entire CollisionRelationship")
                    {
                        targetInstance = CollisionManager.Self.Relationships.FirstOrDefault(item => item.Name == splitVariable[1]);
                    }
                }

                // handled below
                //if (targetInstance == null)
                //{
                //    if (splitVariable[2] == "Entire TileShapeCollection")
                //    {
                //        screen.GetInstance(splitVariable[1], screen, out _, out targetInstance);

                //    }
                //}


                if (targetInstance == null)
                {
                    var collisionRelationship = CollisionManager.Self.Relationships.FirstOrDefault(item =>
                        item.Name == splitVariable[1]);

                    if (collisionRelationship != null)
                    {
                        targetInstance = collisionRelationship;
                    }
                }

                if (targetInstance == null)
                {
                    screen.GetInstance(splitVariable[1], screen, out _, out targetInstance);
                }

            }

            return targetInstance;
        }

        private static bool TryAssignCollisionRelationship(string relationshipName, Models.NamedObjectSave namedObject)
        {
            var handled = false;

            var collisionRelationship = CollisionManager.Self.Relationships.FirstOrDefault(item => item.Name == relationshipName);

            if(collisionRelationship != null)
            {
                T Get<T>(string name) => GlueControl.Models.PropertySaveListExtensions.GetValue<T>(namedObject.Properties, name);

                //DelegateCollision
                var collisionType = Get<int>("CollisionType");

                var firstMass = Get<float>("FirstCollisionMass");
                var secondMass = Get<float>("SecondCollisionMass");
                var elasticity = Get<float>("CollisionElasticity");

                var firstObjectName = Get<string>("FirstCollisionName");
                var secondObjectName = Get<string>("SecondCollisionName");

                object firstObject = null;
                object secondObject = null;

                var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;

                currentScreen.GetInstance($"{firstObjectName}.Unused", currentScreen, out _, out firstObject);
                currentScreen.GetInstance($"{secondObjectName}.Unused", currentScreen, out _, out secondObject);

                var isFirstList = firstObject is IList;
                var isSecondList = secondObject is IList;
                var isSecondShapeCollection = secondObject is ShapeCollection;

                var firstSubCollision = Get<string>("FirstSubCollisionSelectedItem");

                if (firstSubCollision == "<Entire Object>")
                {
                    firstSubCollision = null;
                }

                var secondSubCollision = Get<string>("SecondSubCollisionSelectedItem");

                if (secondSubCollision == "<Entire Object>")
                {
                    secondSubCollision = null;
                }

                var groupPlatformerVariableName = Get<string>("GroundPlatformerVariableName");
                var airPlatformerVariableName = Get<string>("AirPlatformerVariableName");
                var afterDoubleJumpPlatformerVariableName = Get<string>("AfterDoubleJumpPlatformerVariableName");


                var existingRelationshipTypeName = collisionRelationship.GetType().FullName;

                Type desiredRelationshipType = GetDesiredRelationshipType(namedObject);

                // This uses the Glue CollisionPlugin's CollisionType with the following values:
                switch (collisionType)
                {
                    case 0:
                        //NoPhysics = 0,
                        collisionRelationship.SetEventOnlyCollision();
                        handled = true;
                        break;
                    case 1:
                        //MoveCollision = 1,
                        collisionRelationship.SetMoveCollision(firstMass, secondMass);
                        handled = true;
                        break;
                    case 2:
                        //BounceCollision = 2,
                        collisionRelationship.SetBounceCollision(firstMass, secondMass, elasticity);
                        handled = true;
                        break;
                    case 3:
                        //PlatformerSolidCollision = 3,
                        // assume yes, will be no'd later
                        handled = true;
                        break;
                    case 4:
                        //PlatformerCloudCollision = 4,
                        handled = true;
                        break;
                    case 5:
                        break;
                }

                var doFirstSubsMatch =
                    firstSubCollision == collisionRelationship.FirstSubObjectName;
                var doSecondSubsMatch =
                    secondSubCollision == collisionRelationship.SecondSubObjectName;
                var doesFirstMatch =
                    firstObject == collisionRelationship.FirstAsObject;
                var doesSecondMatch =
                    secondObject == collisionRelationship.SecondAsObject;

                if (doFirstSubsMatch == false)
                {
                    handled = false;
                }
                else if (doSecondSubsMatch == false)
                {
                    handled = false;
                }

                if (doesFirstMatch == false)
                {
                    handled = false;
                }
                if (doesSecondMatch == false)
                {
                    handled = false;
                }

                var currentRelationshipType =
                    collisionRelationship.GetType();
                var needsToBeRecreated = desiredRelationshipType != currentRelationshipType;
                if (needsToBeRecreated)
                {
                    handled = false;
                }

                var needsDelegate = currentRelationshipType.Name.StartsWith("Delegate");
                if(needsDelegate)
                {
                    var hasDelegate = currentRelationshipType.GetField("CollisionFunction")
                        ?.GetValue(collisionRelationship) != null;

                    if(!hasDelegate)
                    {
                        handled = false;
                    }
                }

            }

            return handled;
        }

        private static bool TryAssignTileShapeCollection(string tileShapeCollectionName, Models.NamedObjectSave namedObject)
        {
            var handled = false;

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            screen.GetInstance(namedObject.InstanceName, screen, out _, out object tileShapeCollectionAsObject);

            var tileShapeCollection = tileShapeCollectionAsObject as FlatRedBall.TileCollisions.TileShapeCollection;
            if(tileShapeCollection != null)
            {
                T Get<T>(string name) => GlueControl.Models.PropertySaveListExtensions.GetValue<T>(namedObject.Properties, name);
                void ClearShapeCollection() 
                {
                    tileShapeCollection.Visible = false;
                    // What if this was added to the ShapeManager? New versions of generated code don't,
                    // so do we need to bother removing from ShapeManager?
                    tileShapeCollection.Rectangles.Clear();
                }

                var creationOptions = Get<int>("CollisionCreationOptions");

                var isVisible = (namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "Visible")?.Value as bool?) == true;

                var tileSize = Get<float>("CollisionTileSize");

                var leftFill = Get<float>("CollisionFillLeft");
                var topFill = Get<float>("CollisionFillTop");

                var remainderX = leftFill % tileSize;
                var remainderY = topFill % tileSize;

                var widthFill = Get<int>("CollisionFillWidth");
                var heightFill = Get<int>("CollisionFillHeight");

                switch (creationOptions)
                {
                    case 0: // Empty
                        ClearShapeCollection();
                        handled = true;
                        break;
                    case 1: // FillCompletely
                        ClearShapeCollection();

                        tileShapeCollection.GridSize = tileSize;
                        tileShapeCollection.LeftSeedX = remainderX;
                        tileShapeCollection.BottomSeedY = remainderY;
                        tileShapeCollection.SortAxis = FlatRedBall.Math.Axis.X;

                        for(int x = 0; x < widthFill; x++)
                        {
                            for(int y = 0; y < heightFill; y++)
                            {
                                tileShapeCollection.AddCollisionAtWorld(
                                    leftFill + x * tileSize + tileSize / 2.0f,
                                    topFill - y * tileSize - tileSize / 2.0f);
                            }
                        }
                        if(isVisible)
                        {
                            tileShapeCollection.Visible = true;
                        }
                        handled = true;
                        break;
                    case 2: // BorderOutline
                        ClearShapeCollection();

                        tileShapeCollection.GridSize = tileSize;
                        tileShapeCollection.LeftSeedX = remainderX;
                        tileShapeCollection.BottomSeedY = remainderY;
                        tileShapeCollection.SortAxis = FlatRedBall.Math.Axis.X;

                        var borderOutlineType = Get<int>("BorderOutlineType");


                        if (borderOutlineType == 1 /*BorderOutlineType.InnerSize*/)
                        {
                            var innerWidth = Get<float>("InnerSizeWidth");

                            var innerHeight = Get<float>("InnerSizeHeight");

                            var additionalWidth = 2 * tileSize;
                            var additionalHeight = 2 * tileSize;

                            widthFill = FlatRedBall.Math.MathFunctions.RoundToInt(
                                (innerWidth + additionalWidth) / tileSize);
                            heightFill = FlatRedBall.Math.MathFunctions.RoundToInt(
                                (innerHeight + additionalHeight) / tileSize);


                        }

                        for(int x = 0; x < widthFill; x++)
                        {
                            if(x == 0 || x == widthFill - 1)
                            {
                                for(int y = 0; y < heightFill; y++)
                                {
                                    tileShapeCollection.AddCollisionAtWorld(
                                        leftFill + x * tileSize + tileSize / 2.0f,
                                        topFill - y * tileSize - tileSize / 2.0f);

                                }
                            }
                            else
                            {
                                tileShapeCollection.AddCollisionAtWorld(
                                    leftFill + x * tileSize + tileSize / 2.0f,
                                    topFill - tileSize / 2.0f);

                                tileShapeCollection.AddCollisionAtWorld(
                                    leftFill + x * tileSize + tileSize / 2.0f,
                                    topFill - (heightFill - 1) * tileSize - tileSize / 2.0f);
                            }
                        }

                        if (isVisible)
                        {
                            tileShapeCollection.Visible = true;
                        }
                        handled = true;

                        break;
                    case 4: // FromType
                        
                        ClearShapeCollection();

                        var mapName = Get<string>("SourceTmxName");
                        var typeName = Get<string>("CollisionTileTypeName");
                        var removeTiles = Get<bool>("RemoveTilesAfterCreatingCollision");
                        var isMerged = Get<bool>("IsCollisionMerged");
                        if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(typeName))
                        {
                            var map = screen.GetType().GetMethod("GetFile").Invoke(null, new object[] { mapName }) as 
                                FlatRedBall.TileGraphics.LayeredTileMap;

                            if(map == null)
                            {
                                var mapAsObject = FlatRedBall.Instructions.Reflection.LateBinder.GetValueStatic(screen, mapName);
                                map = mapAsObject as FlatRedBall.TileGraphics.LayeredTileMap;
                            }

                            if(map != null)
                            {
                                if (isMerged)
                                {
                                    FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddMergedCollisionFromTilesWithType(
                                        tileShapeCollection, map, typeName);
                                }
                                else
                                {
                                    FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddCollisionFromTilesWithType(
                                        tileShapeCollection, map, typeName, removeTiles);
                                }
                                if (isVisible)
                                {
                                    tileShapeCollection.Visible = true;
                                }

                            }
                        }
                            
                        handled = true;

                        break;
                }
            }

            return handled;
        }

        public static Type GetDesiredRelationshipType(Models.NamedObjectSave namedObject)
        {
            return GetDesiredRelationshipType(namedObject, out _, out _);
        }

        public static Type GetDesiredRelationshipType(Models.NamedObjectSave namedObject, out object firstObject, out object secondObject)
        {
            T Get<T>(string name) => GlueControl.Models.PropertySaveListExtensions.GetValue<T>(namedObject.Properties, name);
            var collisionType = Get<int>("CollisionType");

            var firstObjectName = Get<string>("FirstCollisionName");
            var secondObjectName = Get<string>("SecondCollisionName");

            firstObject = null;
            secondObject = null;

            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;

            currentScreen.GetInstance($"{firstObjectName}.Unused", currentScreen, out _, out firstObject);
            currentScreen.GetInstance($"{secondObjectName}.Unused", currentScreen, out _, out secondObject);

            var isFirstList = firstObject is IList;
            var isSecondList = secondObject is IList;
            var isSecondShapeCollection = secondObject is ShapeCollection;
            var isSecondTileShapeCollection = secondObject is FlatRedBall.TileCollisions.TileShapeCollection;

            Type desiredRelationshipType = null;

            var firstType = firstObject?.GetType();
            var secondType = secondObject?.GetType();

            Type GetStandardCollisionRelationshipType()
            {
                if (isFirstList && isSecondList)
                {
                    return typeof(ListVsListRelationship<,>)
                        .MakeGenericType(firstType.GenericTypeArguments[0], secondType.GenericTypeArguments[0]);
                }
                else if (isFirstList && isSecondShapeCollection)
                {
                    return typeof(ListVsShapeCollectionRelationship<>)
                        .MakeGenericType(firstType.GenericTypeArguments[0]);
                }
                else if(isFirstList && isSecondTileShapeCollection)
                {
                    return typeof(CollidableListVsTileShapeCollectionRelationship<>)
                        .MakeGenericType(firstType.GenericTypeArguments[0]);
                }
                else if (isFirstList)
                {
                    return typeof(ListVsPositionedObjectRelationship<,>)
                        .MakeGenericType(firstType.GenericTypeArguments[0], secondType);
                }
                else if (isSecondList)
                {
                    return typeof(PositionedObjectVsListRelationship<,>)
                        .MakeGenericType(firstType, secondType.GenericTypeArguments[0]);
                }
                else if (isSecondShapeCollection)
                {
                    return typeof(PositionedObjectVsShapeCollection<>)
                        .MakeGenericType(firstType);
                }
                else if(isSecondTileShapeCollection)
                {
                    return typeof(CollidableVsTileShapeCollectionRelationship<>)
                        .MakeGenericType(firstType.GenericTypeArguments[0]);
                }
                else
                {
                    return typeof(PositionedObjectVsPositionedObjectRelationship<,>)
                        .MakeGenericType(firstType, secondType);
                }
            }

            // Get the type here:
            switch (collisionType)
            {
                case 0:
                //NoPhysics = 0,
                case 1:
                //MoveCollision = 1,
                case 2:
                    //BounceCollision = 2,
                    desiredRelationshipType = GetStandardCollisionRelationshipType();
                    break;
                case 3:
                //PlatformerSolidCollision = 3,
                case 4:
                    //PlatformerCloudCollision = 4,

                    if (isFirstList && isSecondList)
                    {
                        desiredRelationshipType = typeof(FlatRedBall.Math.Collision.DelegateListVsListRelationship<,>)
                            .MakeGenericType(firstObject.GetType().GenericTypeArguments[0], secondObject.GetType().GenericTypeArguments[0]);
                    }
                    else if (isFirstList)
                    {
                        desiredRelationshipType = typeof(FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<,>)
                            .MakeGenericType(firstObject.GetType().GenericTypeArguments[0], secondObject.GetType());
                    }
                    else if (isSecondList)
                    {
                        desiredRelationshipType = typeof(FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<,>)
                            .MakeGenericType(firstObject.GetType(), secondObject.GetType().GenericTypeArguments[0]);
                    }

                    break;
                case 5:
                    break;
            }

            return desiredRelationshipType;
        }

        private static object ConvertVariableValue(GlueVariableSetData data)
        {
            var type = data.Type;
            object variableValue = ConvertStringToType(type, data.VariableValue);

            return variableValue;
        }
    
        public static object ConvertStringToType(string type, string variableValue)
        {
            object convertedValue = variableValue;

            if(type == typeof(List<Microsoft.Xna.Framework.Vector2>).ToString())
            {
                convertedValue = JsonConvert.DeserializeObject<List<Microsoft.Xna.Framework.Vector2>>(variableValue);
            }
            else if (type == typeof(List<Point>).ToString())
            {
                convertedValue = JsonConvert.DeserializeObject<List<Point>>(variableValue);
            }
            else
            {
                switch (type)
                {
                    case "float":
                    case nameof(Single):
                    case "System.Single":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = float.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0f;
                        }
                        break;
                    case "int":
                    case nameof(Int32):
                    case "System.Int32":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = int.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0;
                        }
                        break;
                    case "bool":
                    case nameof(Boolean):
                    case "System.Boolean":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = bool.Parse(variableValue.ToLowerInvariant());
                        }
                        else
                        {
                            convertedValue = false;
                        }
                        break;
                    case "double":
                    case nameof(Double):
                    case "System.Double":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = double.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0.0;
                        }
                        break;
                    case "Microsoft.Xna.Framework.Color":
                    case nameof(Microsoft.Xna.Framework.Color):
                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(variableValue).GetValue(null);
                        }
                        else
                        {
                            // do we default to white? that's default for shapes
                            convertedValue = Microsoft.Xna.Framework.Color.White;
                        }
                        break;
                    case "Texture2D":
                    case "Microsoft.Xna.Framework.Graphics.Texture2D":
                        if(!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(
                                variableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                        }
                        else
                        {
                            convertedValue = (Microsoft.Xna.Framework.Graphics.Texture2D)null;
                        }
                        break;
                    case "FlatRedBall.Graphics.Animation.AnimationChainList":
                    case "AnimationChainList":
                        if(!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = FlatRedBallServices.Load<FlatRedBall.Graphics.Animation.AnimationChainList>(
                                variableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                        }
                        else
                        {
                            convertedValue = (FlatRedBall.Graphics.Animation.AnimationChainList)null;
                        }
                        break;
                    case nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode):
                    case "Microsoft.Xna.Framework.Graphics.TextureAddressMode":
                        convertedValue = ToEnum<Microsoft.Xna.Framework.Graphics.TextureAddressMode>(variableValue);
                        break;
                    case nameof(FlatRedBall.Graphics.ColorOperation):
                    case "FlatRedBall.Graphics.ColorOperation":
                        convertedValue = ToEnum<FlatRedBall.Graphics.ColorOperation>(variableValue);

                        break;
                    case nameof(FlatRedBall.Graphics.BlendOperation):
                    case "FlatRedBall.Graphics.BlendOperation":
                        convertedValue = ToEnum<FlatRedBall.Graphics.BlendOperation>(variableValue);

                        break;

                }
                T ToEnum<T>(string asString)
                {
                    if (int.TryParse(variableValue, out int parsedInt))
                    {
                        return (T)(object)parsedInt;
                    }
                    return default(T);
                }
            }

            return convertedValue;
        }
    }
}
