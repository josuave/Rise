using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class BackgroundChoiceTests
{
    [Fact]
    public void Preset_Css_ReturnsGradient()
    {
        BackgroundChoice choice = new PresetBackground("dawn");

        Assert.Contains("linear-gradient", choice.Css, StringComparison.Ordinal);
    }

    [Fact]
    public void None_Css_IsEmpty() => Assert.Equal(string.Empty, BackgroundChoice.None.Css);

    [Fact]
    public void UnknownPreset_Css_IsEmpty()
    {
        BackgroundChoice choice = new PresetBackground("nope");

        Assert.Equal(string.Empty, choice.Css);
    }

    [Fact]
    public void Custom_Css_WrapsTrimmedUrl()
    {
        BackgroundChoice choice = new CustomBackground("  https://example.com/photo.jpg  ");

        Assert.Equal("url('https://example.com/photo.jpg')", choice.Css);
    }

    [Fact]
    public void CustomBlankUrl_Css_IsEmpty()
    {
        BackgroundChoice choice = new CustomBackground("   ");

        Assert.Equal(string.Empty, choice.Css);
    }

    [Fact]
    public void Upload_Css_IsEmpty()
    {
        BackgroundChoice choice = new UploadBackground(new UploadedImage(new string('A', 64), "a.jpg", 1, "image/jpeg"));

        Assert.Equal(string.Empty, choice.Css);
    }

    [Fact]
    public void Default_Css_IsEmpty() => Assert.Equal(string.Empty, default(BackgroundChoice).Css);

    [Fact]
    public void StoredRoundTrip_Preset()
    {
        BackgroundChoice choice = new PresetBackground("mist");

        (string presetId, string? customUrl, UploadedImage? upload) = choice.ToStored();
        var restored = BackgroundChoice.FromStored(presetId, customUrl, upload);

        Assert.True(restored == choice);
    }

    [Fact]
    public void StoredRoundTrip_Custom()
    {
        BackgroundChoice choice = new CustomBackground("https://example.com/photo.jpg");

        (string presetId, string? customUrl, UploadedImage? upload) = choice.ToStored();
        var restored = BackgroundChoice.FromStored(presetId, customUrl, upload);

        Assert.True(restored == choice);
    }

    [Fact]
    public void StoredRoundTrip_Upload()
    {
        BackgroundChoice choice = new UploadBackground(new UploadedImage(new string('A', 64), "a.jpg", 1, "image/jpeg"));

        (string presetId, string? customUrl, UploadedImage? upload) = choice.ToStored();
        var restored = BackgroundChoice.FromStored(presetId, customUrl, upload);

        Assert.True(restored == choice);
    }

    [Fact]
    public void FromStored_UnknownPreset_KeepsId()
    {
        var restored = BackgroundChoice.FromStored("nope", null, null);

        Assert.True(restored is PresetBackground { Id: "nope" });
        Assert.Equal(string.Empty, restored.Css);
    }

    [Fact]
    public void FromStored_UploadWithoutDescriptor_RepairsToNone()
    {
        var restored = BackgroundChoice.FromStored(BackgroundPresets.Upload, null, null);

        Assert.True(restored == BackgroundChoice.None);
    }

    [Fact]
    public void FromStored_CustomWithoutUrl_IsBlankCustom()
    {
        var restored = BackgroundChoice.FromStored(BackgroundPresets.Custom, null, null);

        Assert.True(restored is CustomBackground { Url: "" });
        Assert.Equal(string.Empty, restored.Css);
    }

    [Fact]
    public void Equality_SameCaseSameValue_IsEqual()
    {
        Assert.Equal<BackgroundChoice>(new PresetBackground("dawn"), new PresetBackground("dawn"));
        Assert.NotEqual<BackgroundChoice>(new PresetBackground("dawn"), new PresetBackground("mist"));
        Assert.NotEqual<BackgroundChoice>(new PresetBackground("dawn"), new CustomBackground("https://example.com/x.jpg"));
    }
}
