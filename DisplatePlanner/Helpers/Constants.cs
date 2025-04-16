using DisplatePlanner.Models;

namespace DisplatePlanner.Helpers;

public static class Constants
{
    public static readonly List<PlateInfo> AllLuminos =
    [
        new(
              ExternalId: 7481498,
              StartDate: new DateTime(2024, 12, 6, 11, 0, 0),
              Title: "The Last City",
              Image: new Image(
                  X2: "https://static.displate.com/460x640/lumino/2024-11-29/9f7c61d2482bc16c7f1e0748374da726_1f8d75da5f8af97dec93d3c1ff068de8.jpg"
            )
          ),
        new(
            ExternalId: 7481360,
            StartDate: new DateTime(2024, 10, 25, 10, 0, 0),
            Title: "Order 66",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2024-10-18/a0d5277b349d8ee96e914275b85e366f_c917d30caf83aedbc222c9b870727773.jpg"
            )
        ),
        new(
            ExternalId: 7481455,
            StartDate: new DateTime(2024, 11, 9, 11, 0, 0),
            Title: "Janna's Temple",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2024-10-29/55ad5d58ad4ba39e5dbf41e84d8fa4cd_a1c3edb5f30edbead9dfcd9a621fe62c.jpg"
            )
        ),
        new(
            ExternalId: 7354461,
            StartDate: new DateTime(2024, 8, 2, 10, 0, 0),
            Title: "Alduin's Wrath",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2024-07-25/a77e36e6d0bcad4a7c8c014b0b508441_59ba66606c359d14e59b116c42b3d51c.jpg"
            )
        ),
        new(
            ExternalId: 7315341,
            StartDate: new DateTime(2024, 5, 17, 10, 0, 0),
            Title: "Presence of the Baron",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2024-05-10/010b74433795da48dd699f8a4b4ce02c_e46a1222f0f383963b3b45bf38200939.jpg"
            )
        ),
        new(
            ExternalId: 7253513,
            StartDate: new DateTime(2024, 4, 19, 10, 0, 0),
            Title: "In Leshen's Domain",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2024-04-12/3eb437768e5488a22b16a6174558ce4f_856498ce6250cbca02131e262505c11c.jpg"
            )
        ),
        new(
            ExternalId: 6881264,
            StartDate: new DateTime(2023, 8, 13, 10, 0, 0),
            Title: "Crystal Catacombs",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-11-08/356f6998f0731ff82dd30e61ef4b51aa_6d149a460652f01bf0837bb41630ecbf.jpg"
            )
        ),
        new(
            ExternalId: 6678090,
            StartDate: new DateTime(2023, 10, 6, 10, 0, 0),
            Title: "The Dragon God Bahamut",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-09-29/368b3bec3be89f6c2d7db86e43690e73_58d2c32fada5896de4399fad7c2bf6ff.jpg"
            )
        ),
        new(
            ExternalId: 6555450,
            StartDate: new DateTime(2023, 8, 18, 10, 0, 0),
            Title: "The Ancient One",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-08-11/32477fdc15817321e3e2a474f6de6029_5be8a329122e9a58879ddc52f8473adf.jpg"
            )
        ),
        new(
            ExternalId: 6355812,
            StartDate: new DateTime(2023, 6, 16, 10, 0, 0),
            Title: "Guardians vs Thanos",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-06-16/69558b3a76b0702783df6707949d5629_38274a748d46878f48343e0cee0dfdfd.jpg"
            )
        ),
        new(
            ExternalId: 6355809,
            StartDate: new DateTime(2023, 7, 14, 10, 0, 0),
            Title: "Defying the Gods",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-07-17/24054d4f711e85001f43626e271fde30_0989134ab47b34ac75d25b5e40baf31d.jpg"
            )
        ),
        new(
            ExternalId: 6355805,
            StartDate: new DateTime(2023, 5, 19, 10, 0, 0),
            Title: "Cosmic Deer",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-05-18/490dd8608860233ee41ffc45af8e6353_f0a84566bbce5ca105401479641df82f.jpg"
            )
        ),
        new(
            ExternalId: 6355796,
            StartDate: new DateTime(2023, 5, 5, 10, 0, 0),
            Title: "The Ambush",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-05-04/c4d6bb96aebd81564b6c7560cddd8fa1_ed0a9d9e696a2c66831456d8b4fc5bd6.jpg"
            )
        ),
        new(
            ExternalId: 6355789,
            StartDate: new DateTime(2023, 4, 21, 10, 0, 0),
            Title: "Leaving Earth",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-04-20/0bdb9a65d592a4b4a0e05a64f187e4ff_567bd45dbf332183a486f439d6f06a26.jpg"
            )
        ),
        new(
            ExternalId: 6355779,
            StartDate: new DateTime(2023, 4, 7, 10, 0, 0),
            Title: "Seduced by Night City",
            Image: new Image(
                X2: "https://static.displate.com/460x640/lumino/2023-04-04/a8bd8763f567a461dd9a8ce008037eef_7eff10571e51d1d4299e9482196e6c78.jpg"
            )
        ),
    ];
}