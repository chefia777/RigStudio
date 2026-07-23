using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Application.Rigs;
using SpriteRigStudio.Application.Skeletons;
using SpriteRigStudio.Application.Animations;
using SpriteRigStudio.Application.Exporting;
using SpriteRigStudio.Application.Validation;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Parts;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Transforms;
using SpriteRigStudio.Infrastructure.FileSystem;
using SpriteRigStudio.Infrastructure.Hashing;
using SpriteRigStudio.Infrastructure.Persistence;
using SpriteRigStudio.Infrastructure.Serialization;
using SpriteRigStudio.Rendering.Abstractions;
using SpriteRigStudio.Rendering.Images;
using SpriteRigStudio.Rendering.Masks;
using SpriteRigStudio.Rendering.Poses;
using SpriteRigStudio.Rendering.Composition;
using SpriteRigStudio.Rendering.Spritesheets;

namespace SpriteRigStudio.Integration.Tests;

public class FullWorkflowTests
{
    private readonly ITestOutputHelper _output;

    public FullWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CreateProject_AddSkeleton_AddCharacter_AddAnimation_SaveAndLoad_ExportsSpritesheet()
    {
        // ── Setup infrastructure ─────────────────────────────────
        var fileSystem = new WindowsFileSystem();
        var hashProvider = new Sha256HashProvider();
        var serializer = new ProjectSerializer();
        var loggerFactory = LoggerFactory.Create(b => { });
        var atomicWriter = new AtomicProjectFileWriter(fileSystem, loggerFactory.CreateLogger<AtomicProjectFileWriter>());
        var repository = new ProjectRepository(fileSystem, hashProvider, serializer, atomicWriter,
            loggerFactory.CreateLogger<ProjectRepository>());
        var projectService = new ProjectService(repository, loggerFactory.CreateLogger<ProjectService>());
        var skeletonService = new SkeletonService(loggerFactory.CreateLogger<SkeletonService>());
        var rigService = new RigService(loggerFactory.CreateLogger<RigService>());
        var animationService = new AnimationService(loggerFactory.CreateLogger<AnimationService>());
        var exportService = new ExportService(loggerFactory.CreateLogger<ExportService>());
        var projectValidator = new ProjectValidator();

        // ── 1. Create project ───────────────────────────────────
        var tempDir = Path.Combine(Path.GetTempPath(), "SRS_IntegrationTest_" + Guid.NewGuid().ToString("N"));
        var createResult = await projectService.CreateProjectAsync("Test Warrior", tempDir);
        Assert.True(createResult.IsSuccess, $"Create project failed: {createResult.ErrorMessage}");
        var project = createResult.Project!;
        _output.WriteLine($"Project created at: {tempDir}");

        // ── 2. Project should have default humanoid skeleton ────
        Assert.Single(project.Skeletons);
        var skeleton = project.Skeletons.Values.First();
        Assert.Equal("Humanoid", skeleton.Name);
        Assert.Equal(24, skeleton.Bones.Count);
        Assert.False(skeleton.HasCycles());

        // ── 3. Create a character rig ────────────────────────────
        var character = rigService.CreateCharacterRig("Test Warrior", skeleton.SkeletonId);
        character.SetupTransform.Position = new Vector2D(150, 200);
        character.SetupTransform.UniformScale = 1.0;
        character.GroundAnchor = new Vector2D(150, 20);
        project.CharacterRigs[character.CharacterRigId.ToKeyString()] = character;
        _output.WriteLine($"Character created: {character.Name}");

        // ── 4. Create an idle animation ──────────────────────────
        var idleAnim = animationService.CreateAnimation("idle", skeleton.SkeletonId, fps: 12, durationSeconds: 1.0);
        project.Animations[idleAnim.AnimationId.ToKeyString()] = idleAnim;

        // Add keyframes to chest bone for breathing
        var chestBone = skeleton.Bones.Values.First(b => b.Role == BoneRole.Chest);
        var breatheIn = new TransformKeyframe
        {
            TimeSeconds = 0,
            Position = Vector2D.Zero,
            RotationDegrees = 0,
            Scale = new Vector2D(1, 1),
            Interpolation = InterpolationMode.Linear
        };
        var breatheOut = new TransformKeyframe
        {
            TimeSeconds = 0.5,
            Position = Vector2D.Zero,
            RotationDegrees = 2,
            Scale = new Vector2D(1, 1),
            Interpolation = InterpolationMode.Linear
        };
        var breatheEnd = new TransformKeyframe
        {
            TimeSeconds = 1.0,
            Position = Vector2D.Zero,
            RotationDegrees = 0,
            Scale = new Vector2D(1, 1),
            Interpolation = InterpolationMode.Linear
        };
        animationService.AddKeyframe(idleAnim, chestBone.BoneId, breatheIn);
        animationService.AddKeyframe(idleAnim, chestBone.BoneId, breatheOut);
        animationService.AddKeyframe(idleAnim, chestBone.BoneId, breatheEnd);

        _output.WriteLine($"Animation created: {idleAnim.Name} with {idleAnim.FrameCount} frames");

        // ── 5. Create an export profile ─────────────────────────
        var profile = new ExportProfile
        {
            ExportProfileId = ExportProfileId.New(),
            Name = "4x4 300",
            FrameWidth = 300,
            FrameHeight = 300,
            Columns = 4,
            Rows = 4,
            FrameRate = 12,
            BackgroundMode = BackgroundMode.Transparent,
            SamplingMode = SamplingMode.NearestNeighbor,
            AnchorPixel = new Vector2D(150, 260),
            OverflowPolicy = OverflowPolicy.FailExport
        };
        project.ExportProfiles[profile.ExportProfileId.ToKeyString()] = profile;

        // ── 6. Save the project ─────────────────────────────────
        var saveResult = await projectService.SaveProjectAsync(project);
        Assert.True(saveResult.IsSuccess, $"Save failed: {saveResult.ErrorMessage}");
        _output.WriteLine($"Project saved to: {saveResult.FilePath}");

        // ── 7. Close and reopen ─────────────────────────────────
        var openResult = await projectService.OpenProjectAsync(tempDir);
        Assert.True(openResult.IsSuccess, $"Open failed: {openResult.ErrorMessage}");
        var reloadedProject = openResult.Project!;
        Assert.Equal("Test Warrior", reloadedProject.Name);
        Assert.Single(reloadedProject.Skeletons);
        Assert.Single(reloadedProject.CharacterRigs);
        Assert.Single(reloadedProject.Animations);
        Assert.Single(reloadedProject.ExportProfiles);
        _output.WriteLine("Project reopened successfully with all data preserved.");

        // ── 8. Validate project ─────────────────────────────────
        var validation = projectValidator.ValidateProject(reloadedProject);
        Assert.True(validation.IsValid, $"Validation errors: {string.Join(", ", validation.Errors.Select(e => e.Message))}");
        _output.WriteLine("Project validation passed.");

        // ── 9. Validate export ──────────────────────────────────
        var reloadedChar = reloadedProject.CharacterRigs.Values.First();
        var reloadedAnim = reloadedProject.Animations.Values.First();
        var exportValidation = exportService.ValidateExport(reloadedProject, reloadedChar, reloadedAnim, profile);
        Assert.True(exportValidation.IsValid, $"Export validation: {string.Join(", ", exportValidation.Errors.Select(e => e.Message))}");

        // ── 10. Export via CLI-style pipeline ───────────────────
        var poseEvaluator = new RigPoseEvaluator();
        var imageDecoder = new SkiaImageDecoder();
        var imageEncoder = new SkiaImageEncoder();
        var maskRasterizer = new SkiaMaskRasterizer();
        var frameRenderer = new FrameRenderer(maskRasterizer);
        var sheetComposer = new SpritesheetComposer();

        // Create a simple test image for the character
        var sourcesDir = Path.Combine(tempDir, "Sources");
        Directory.CreateDirectory(sourcesDir);
        var testImagePath = Path.Combine(sourcesDir, "test_warrior.png");
        CreateTestPng(testImagePath, 64, 64);
        reloadedChar.SourceArtwork = "Sources/test_warrior.png";

        // Create a simple part bound to root bone
        var rootBone = reloadedProject.Skeletons[reloadedChar.SkeletonId.ToKeyString()].GetRootBone()!;
        var part = rigService.CreatePart(reloadedChar, "body", rootBone.BoneId,
            new Vector2D(32, 32), SourceType.FlattenedImageMask, reloadedChar.SourceArtwork);

        var paths = exportService.GetOutputPaths(tempDir, reloadedChar.Name, reloadedAnim.Name, profile);
        Directory.CreateDirectory(paths.ExportsDirectory);

        // Evaluate, render, and compose
        var frameCount = animationService.GetSampledFrameCount(reloadedAnim, profile.FrameRate);
        _output.WriteLine($"Exporting {frameCount} frames...");
        Assert.Equal(12, frameCount); // 1s at 12fps, looping = 12 frames

        // Decode the source image
        var decoded = await imageDecoder.DecodeAsync(testImagePath);
        Assert.True(decoded.IsSuccess);

        // Render frames
        var frames = new List<byte[]>();
        for (int i = 0; i < frameCount; i++)
        {
            var time = animationService.GetSampleTime(reloadedAnim, i, profile.FrameRate);
            var pose = poseEvaluator.EvaluateAnimationPose(
                reloadedProject.Skeletons[reloadedChar.SkeletonId.ToKeyString()],
                reloadedChar, reloadedAnim, time);

            var decodedImages = new Dictionary<string, DecodedImageInfo>
            {
                [reloadedChar.SourceArtwork!] = decoded.Value!
            };

            var framePixels = frameRenderer.RenderFrame(pose, reloadedChar, profile, decodedImages);
            Assert.NotNull(framePixels);
            Assert.Equal(profile.FrameWidth * profile.FrameHeight * 4, framePixels.Length);
            frames.Add(framePixels);
        }

        // Compose spritesheet
        var sheetPixels = sheetComposer.ComposeSpritesheet(frames,
            profile.Columns, profile.Rows, profile.FrameWidth, profile.FrameHeight);
        Assert.NotNull(sheetPixels);
        Assert.Equal(profile.SheetWidth * profile.SheetHeight * 4, sheetPixels.Length);

        // Write PNG
        var encodeResult = await imageEncoder.EncodePngToFileAsync(paths.SpritesheetPath,
            profile.SheetWidth, profile.SheetHeight, sheetPixels);
        Assert.True(encodeResult.IsSuccess, $"PNG encode failed: {encodeResult.ErrorMessage}");
        Assert.True(File.Exists(paths.SpritesheetPath));
        _output.WriteLine($"Spritesheet exported: {paths.SpritesheetPath}");

        var fileInfo = new FileInfo(paths.SpritesheetPath);
        Assert.True(fileInfo.Length > 0);
        Assert.Equal(1200, profile.SheetWidth);
        Assert.Equal(1200, profile.SheetHeight);

        _output.WriteLine("=== INTEGRATION TEST PASSED ===");

        // Cleanup
        Directory.Delete(tempDir, recursive: true);
    }

    private static void CreateTestPng(string path, int width, int height)
    {
        using var bitmap = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(new SkiaSharp.SKColor(255, 0, 0, 255)); // Red square

        // Draw a simple stick figure
        using var paint = new SkiaSharp.SKPaint
        {
            Color = new SkiaSharp.SKColor(0, 0, 0, 255),
            StrokeWidth = 3,
            Style = SkiaSharp.SKPaintStyle.Stroke
        };
        // Head
        canvas.DrawCircle(width / 2, height / 4, 8, paint);
        // Body
        canvas.DrawLine(width / 2, height / 4 + 8, width / 2, height * 3 / 4, paint);
        // Arms
        canvas.DrawLine(width / 4, height / 2, width * 3 / 4, height / 2, paint);
        // Legs
        canvas.DrawLine(width / 2, height * 3 / 4, width / 4, height, paint);
        canvas.DrawLine(width / 2, height * 3 / 4, width * 3 / 4, height, paint);

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
