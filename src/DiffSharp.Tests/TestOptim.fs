namespace Tests

open NUnit.Framework
open DiffSharp
open DiffSharp.Model
open DiffSharp.Data
open DiffSharp.Optim

[<TestFixture>]
type TestOptim () =

    [<SetUp>]
    member this.Setup () =
        ()

    [<Test>]
    member this.TestOptimSGD () =
        // Trains a linear regressor
        let n, din, dout = 64, 100, 10
        let inputs  = dsharp.randn([n; din])
        let targets = dsharp.randn([n; dout])
        let dataset = TensorDataset(inputs, targets)
        let dataloader = dataset.loader(8, shuffle=true)
        let net = Linear(din, dout)

        let lr, mom, epochs = 1e-2, 0.9, 250
        let optimizer = SGD(net, learningRate=dsharp.tensor(lr), momentum=dsharp.tensor(mom), nesterov=true)
        for _ in 0..epochs do
            for _, inputs, targets in dataloader.epoch() do
                net.reverseDiff()
                let y = net.forward(inputs)
                let loss = dsharp.mseLoss(y, targets)
                loss.reverse()
                optimizer.step()
        let y = net.forward inputs
        Assert.True(targets.allclose(y, 0.1, 0.1))