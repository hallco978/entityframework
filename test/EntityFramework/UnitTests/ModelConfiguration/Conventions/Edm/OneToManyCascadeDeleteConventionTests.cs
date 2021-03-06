// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using Xunit;

    public sealed class OneToManyCascadeDeleteConventionTests
    {
        [Fact]
        public void Apply_should_not_add_action_when_self_reference()
        {
            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);
            associationType.SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace));
            associationType.TargetEnd = new AssociationEndMember("T", associationType.SourceEnd.GetEntityType());

            (new OneToManyCascadeDeleteConvention())
                .Apply(associationType, new DbModel(new EdmModel(DataSpace.CSpace), null));

            Assert.Equal(OperationAction.None, associationType.SourceEnd.DeleteBehavior);
            Assert.Equal(OperationAction.None, associationType.TargetEnd.DeleteBehavior);
        }

        [Fact]
        public void Apply_should_not_add_action_when_has_existing_action()
        {
            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);
            associationType.SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace));
            associationType.TargetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace));

            associationType.SourceEnd.DeleteBehavior = OperationAction.Cascade;

            (new OneToManyCascadeDeleteConvention())
                .Apply(associationType, new DbModel(new EdmModel(DataSpace.CSpace), null));

            Assert.Equal(OperationAction.Cascade, associationType.SourceEnd.DeleteBehavior);
            Assert.Equal(OperationAction.None, associationType.TargetEnd.DeleteBehavior);
        }

        [Fact]
        public void Apply_should_add_action_when_is_required_to_many()
        {
            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);
            associationType.SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace));
            associationType.TargetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace));

            associationType.SourceEnd.RelationshipMultiplicity = RelationshipMultiplicity.One;
            associationType.TargetEnd.RelationshipMultiplicity = RelationshipMultiplicity.Many;

            (new OneToManyCascadeDeleteConvention())
                .Apply(associationType, new DbModel(new EdmModel(DataSpace.CSpace), null));

            Assert.Equal(OperationAction.Cascade, associationType.SourceEnd.DeleteBehavior);
        }

        [Fact]
        public void Apply_should_add_action_when_is_many_to_required()
        {
            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);
            associationType.SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace));
            associationType.TargetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace));

            associationType.SourceEnd.RelationshipMultiplicity = RelationshipMultiplicity.Many;
            associationType.TargetEnd.RelationshipMultiplicity = RelationshipMultiplicity.One;

            (new OneToManyCascadeDeleteConvention())
                .Apply(associationType, new DbModel(new EdmModel(DataSpace.CSpace), null));

            Assert.Equal(OperationAction.Cascade, associationType.TargetEnd.DeleteBehavior);
        }
    }
}
