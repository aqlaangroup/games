// Subject masks from macOS Vision (the same subject lifting as Photos), offline. Arguments: pairs of <image> <mask.png>.
// Built and run by lift.py.
import Vision
import CoreImage

let args = Array(CommandLine.arguments.dropFirst())
let context = CIContext()
var i = 0
while i + 1 < args.count {
    let input = URL(fileURLWithPath: args[i]), output = URL(fileURLWithPath: args[i + 1])
    i += 2
    let handler = VNImageRequestHandler(url: input)
    let request = VNGenerateForegroundInstanceMaskRequest()
    do {
        try handler.perform([request])
        guard let result = request.results?.first else { print("no subject:", input.path); continue }
        let mask = try result.generateScaledMaskForImage(forInstances: result.allInstances, from: handler)
        try context.writePNGRepresentation(of: CIImage(cvPixelBuffer: mask), to: output, format: .L8,
                                           colorSpace: CGColorSpaceCreateDeviceGray())
    } catch { print("failed:", input.path, error) }
}
