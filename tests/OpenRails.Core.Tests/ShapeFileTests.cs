using OpenRails.Core.Formats;
using Xunit;

namespace OpenRails.Core.Tests;

public class ShapeFileTests
{
    [Fact]
    public void Parses_points_uvs_images_and_first_lod_triangles()
    {
        var shape = ShapeFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0s1t______
            shape (
                points ( 3
                    point ( 0 0 0 )
                    point ( 1 0 0 )
                    point ( 0 1 0 )
                )
                uv_points ( 3
                    uv_point ( 0 0 )
                    uv_point ( 1 0 )
                    uv_point ( 0 1 )
                )
                images ( 1
                    image ( body.ace )
                )
                prim_states ( 1
                    prim_state MAIN ( 00000000 0
                        tex_idxs ( 1 0 )
                    )
                )
                lod_controls ( 1
                    lod_control ( 1
                        sub_objects ( 1
                            sub_object (
                                vertices ( 3
                                    vertex ( 00000000 0 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                                    vertex ( 00000000 1 0 FFFFFFFF FF000000 vertex_uvs ( 1 1 ) )
                                    vertex ( 00000000 2 0 FFFFFFFF FF000000 vertex_uvs ( 1 2 ) )
                                )
                                primitives ( 1
                                    prim_state_idx ( 0 )
                                    indexed_trilist (
                                        vertex_idxs ( 3 0 1 2 )
                                        flags ( 3 0 0 0 )
                                    )
                                )
                            )
                        )
                    )
                )
            )
            """);

        Assert.Equal(3, shape.Points.Count);
        Assert.Equal(3, shape.TextureCoordinates.Count);
        Assert.Single(shape.ImageNames);
        Assert.Equal("body.ace", shape.ImageNames[0]);
        Assert.Single(shape.PrimitiveGroups);
        Assert.Equal(1, shape.TriangleCount);
        Assert.Equal("body.ace", shape.PrimitiveGroups[0].TextureName);
        Assert.Equal(3, shape.PrimitiveGroups[0].Vertices.Count);
        Assert.Equal(new System.Numerics.Vector2(1, 0),
            shape.PrimitiveGroups[0].Vertices[1].TextureCoordinate);
        Assert.Equal(0f, shape.BoundsMin.X);
        Assert.Equal(1f, shape.BoundsMax.X);
        Assert.Equal(1f, shape.BoundsMax.Y);
    }

    [Fact]
    public void Parses_matrices_before_images_and_applies_translation()
    {
        var shape = ShapeFile.Parse(
            """
            shape (
                points ( 3
                    point ( 1 0 0 )
                    point ( 2 0 0 )
                    point ( 1 1 0 )
                )
                uv_points ( 3
                    uv_point ( 0 0 )
                    uv_point ( 1 0 )
                    uv_point ( 0 1 )
                )
                matrices ( 1
                    matrix BODY ( 1 0 0 0 1 0 0 0 1 10 0 0 )
                )
                images ( 1 image ( body.dds ) )
                prim_states ( 1
                    prim_state BODY_main ( 00000000 0 tex_idxs ( 1 0 ) )
                )
                lod_controls ( 1 lod_control ( 1 sub_objects ( 1
                    sub_object (
                        vertices ( 3
                            vertex ( 00000000 0 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                            vertex ( 00000000 1 0 FFFFFFFF FF000000 vertex_uvs ( 1 1 ) )
                            vertex ( 00000000 2 0 FFFFFFFF FF000000 vertex_uvs ( 1 2 ) )
                        )
                        primitives ( 1
                            prim_state_idx ( 0 )
                            indexed_trilist ( vertex_idxs ( 3 0 1 2 ) )
                        )
                    )
                ) ) )
            )
            """);

        Assert.Single(shape.Matrices);
        var group = Assert.Single(shape.PrimitiveGroups);
        Assert.Equal(3, group.Vertices.Count);
        Assert.Equal(11f, group.Vertices[0].Position.X);
    }

    [Fact]
    public void Parses_multiple_primitive_pairs_from_doubled_block_count()
    {
        var shape = ShapeFile.Parse(
            """
            shape (
                points ( 4
                    point ( 0 0 0 )
                    point ( 1 0 0 )
                    point ( 0 1 0 )
                    point ( 0 0 1 )
                )
                uv_points ( 1 uv_point ( 0 0 ) )
                images ( 1 image ( body.dds ) )
                prim_states ( 1 prim_state MAIN ( 00000000 0 tex_idxs ( 1 0 ) ) )
                lod_controls ( 1 lod_control ( 1 sub_objects ( 1
                    sub_object (
                        vertices ( 4
                            vertex ( 00000000 0 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                            vertex ( 00000000 1 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                            vertex ( 00000000 2 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                            vertex ( 00000000 3 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                        )
                        primitives ( 4
                            prim_state_idx ( 0 )
                            indexed_trilist ( vertex_idxs ( 3 0 1 2 ) )
                            prim_state_idx ( 0 )
                            indexed_trilist ( vertex_idxs ( 3 0 2 3 ) )
                        )
                    )
                ) ) )
            )
            """);

        Assert.Equal(2, shape.TriangleCount);
        Assert.Equal(6, Assert.Single(shape.PrimitiveGroups).Vertices.Count);
    }

    [Fact]
    public void Applies_wheel_matrix_after_bogie_matrix()
    {
        var shape = ShapeFile.Parse(
            """
            shape (
                points ( 1 point ( 0 0 0 ) )
                uv_points ( 1 uv_point ( 0 0 ) )
                matrices ( 2
                    matrix BOGIE2 ( 1 0 0 0 1 0 0 0 1 0 0.753647 -3.345 )
                    matrix WHEELS21 ( 1 0 0 0 1 0 0 0 1 0 -0.282787 1.00167 )
                )
                images ( 1 image ( bogie.dds ) )
                prim_states ( 1 prim_state WHEELS21_bogie ( 00000000 0 tex_idxs ( 1 0 ) ) )
                lod_controls ( 1 lod_control ( 1 sub_objects ( 1
                    sub_object (
                        vertices ( 1
                            vertex ( 00000000 0 0 FFFFFFFF FF000000 vertex_uvs ( 1 0 ) )
                        )
                        primitives ( 2
                            prim_state_idx ( 0 )
                            indexed_trilist ( vertex_idxs ( 3 0 0 0 ) )
                        )
                    )
                ) ) )
            )
            """);

        var position = Assert.Single(shape.PrimitiveGroups).Vertices[0].Position;
        Assert.Equal(0.470860f, position.Y, 3);
        Assert.Equal(-2.34333f, position.Z, 3);
    }


}
